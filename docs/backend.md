# Backend

.NET 10, com `Nullable` e `ImplicitUsings` ligados em todos os projetos. As camadas e o fluxo da requisição estão em
[Arquitetura](arquitetura.md). Aqui fica **como escrever cada peça**, com os modelos de Empresas e Usuários.

## Onde cada coisa fica

| Peça | Pasta | Nome |
| --- | --- | --- |
| Entidade | `Domain/Entities/` | `Empresa` |
| Regra pura reutilizável | `Domain/Common/` | `Cnpj` (static) |
| Interface de repositório | `Domain/Interfaces/` | `IEmpresaRepository` |
| Exceções de negócio | `Domain/Exceptions/DomainException.cs` | `NotFoundException`, `ConflictException`, `BusinessRuleException` |
| Interface de service | `Application/Interfaces/` | `IEmpresaService` |
| Service | `Application/Services/` | `EmpresaService` |
| DTOs | `Application/DTOs/<Entidade>sDtos/` | `CriarEmpresaRequest`, `AtualizarEmpresaRequest`, `EmpresaResponse` |
| Validators | `Application/Validators/<Entidade>sValidators/` | `CriarEmpresaRequestValidator` |
| Mapeamento EF | `Infrastructure/DatabaseConfigs/` | `EmpresaConfig` |
| Repositório | `Infrastructure/Repositories/` | `EmpresaRepository` |
| Migrations | `Infrastructure/Migrations/` | geradas pelo `dotnet ef` |
| Controller | `API/Controllers/` | `EmpresasController` |
| Filtros e middlewares | `API/Filters/`, `API/Logging/` | `ValidationFilter`, `CorrelationIdMiddleware` |

Um namespace por pasta (file-scoped): `namespace FluxusManager.Application.Services;`.

## Nomenclatura

- **Domínio em português**, técnico em inglês (ver [regras gerais](README.md#regras-que-valem-para-tudo)).
- Métodos de service seguem o caso de uso: `CriarAsync`, `ObterAsync`, `ListarAsync`, `AtualizarAsync`, `AtivarAsync`, `InativarAsync`.
- Métodos herdados de `IRepository<T>` ficam em inglês (`GetByIdAsync`, `ListAsync`, `Add`). As consultas próprias de
  um repositório ficam em português e dizem o que respondem: `CnpjEmUsoAsync`, `EmailEmUsoAsync`.
- Todo método assíncrono termina em `Async` e recebe `CancellationToken cancellationToken = default` como último parâmetro.
- Interfaces com `I`; campos privados com `_`; constantes em PascalCase.
- Tabelas e colunas: plural em português e `snake_case` (`empresas`, `razao_social`), gerados pela convenção do EF.

## Estilo de código

- **Construtor primário** para injeção de dependência: `public class EmpresaService(IEmpresaRepository empresas, IUnitOfWork unitOfWork) : IEmpresaService`.
- Membros de uma linha com `=>`. Métodos curtos, com uma linha em branco separando buscar, agir e gravar.
- `record` para DTOs e resultados imutáveis (`PagedResult<T>`). `class` para entidades e services.
- **Comentários `/// <summary>`** em português nos tipos e membros públicos quando há algo a explicar: uma regra,
  uma restrição ou uma decisão. Não repita o nome do método.
- O CI roda `dotnet format --verify-no-changes`: rode `dotnet format` antes de commitar.

## Entidades

```csharp
/// <summary>Empresa cliente do sistema; é o tenant dos dados multi-tenant. O CNPJ não muda depois do cadastro.</summary>
public class Empresa : BaseEntity
{
    public string RazaoSocial { get; private set; }
    public string? NomeFantasia { get; private set; }
    public string Cnpj { get; private set; }
    public bool Ativo { get; private set; } = true;

    public Empresa(string razaoSocial, string? nomeFantasia, string cnpj)
    {
        RazaoSocial = razaoSocial;
        NomeFantasia = nomeFantasia;
        Cnpj = cnpj;
    }

    public void Atualizar(string razaoSocial, string? nomeFantasia) { ... }
    public void Ativar() => Ativo = true;
    public void Inativar() => Ativo = false;
}
```

- **Herdam `BaseEntity`**: `Id` (Guid v7, gerado na criação), `CriadoEm`, `AtualizadoEm` e `Excluido`. As datas e a
  exclusão são preenchidas pelo `AppDbContext`. Nunca atribua na mão.
- **Setters privados.** O estado muda por métodos com nome de negócio (`Atualizar`, `Ativar`, `Inativar`), nunca
  por propriedade pública.
- **Construtor com os campos obrigatórios.** A entidade nasce válida (ex.: `Ativo = true`).
- Campo que não pode mudar depois do cadastro (ex.: CNPJ) **não entra** no método `Atualizar`.
- Entidade de um tenant implementa `ITenantEntity`. O `TenantId` é preenchido ao salvar.
- A entidade não conhece EF, DTO nem HTTP.

## Repositórios

```csharp
// Domain/Interfaces
public interface IEmpresaRepository : IRepository<Empresa>
{
    /// <summary>Indica se o CNPJ (normalizado) já pertence a alguma empresa não excluída.</summary>
    Task<bool> CnpjEmUsoAsync(string cnpj, CancellationToken cancellationToken = default);
}

// Infrastructure/Repositories
public class EmpresaRepository(AppDbContext context) : RepositoryBase<Empresa>(context), IEmpresaRepository
{
    public Task<bool> CnpjEmUsoAsync(string cnpj, CancellationToken cancellationToken = default)
        => Set.AnyAsync(e => e.Cnpj == cnpj, cancellationToken);
}
```

- `IRepository<T>` já dá `GetByIdAsync`, `ListAsync`, `ExistsAsync`, `Add`, `Update` e `Remove`.
  O repositório específico **só acrescenta consultas próprias**.
- Entidade sem consultas próprias usa o `IRepository<T>` genérico (já registrado); não crie repositório vazio.
- **Repositório nunca grava.** Nada de `SaveChanges`: quem confirma é o `IUnitOfWork`, chamado pelo service.
- Listagens com filtro usam `ToPagedResultAsync(query, page, pageSize, ct)`, que ordena por `CriadoEm, Id` e limita
  `pageSize` a 100. Consultas só de leitura usam `AsNoTracking()`.
- Os filtros globais (tenant e exclusão lógica) já estão aplicados. Não repita `!e.Excluido` nas consultas.

## Services

```csharp
public class EmpresaService(IEmpresaRepository empresas, IUnitOfWork unitOfWork) : IEmpresaService
{
    public async Task<EmpresaResponse> CriarAsync(CriarEmpresaRequest request, CancellationToken cancellationToken = default)
    {
        var cnpj = Cnpj.Normalizar(request.Cnpj);

        if (await empresas.CnpjEmUsoAsync(cnpj, cancellationToken))
            throw new ConflictException($"Já existe uma empresa com o CNPJ {Cnpj.Formatar(cnpj)}.");

        var empresa = new Empresa(request.RazaoSocial.Trim(), NomeFantasia(request.NomeFantasia), cnpj);

        empresas.Add(empresa);
        await unitOfWork.CommitAsync(cancellationToken);

        return EmpresaResponse.DeEntidade(empresa);
    }

    private async Task<Empresa> BuscarAsync(Guid id, CancellationToken cancellationToken)
        => await empresas.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Empresa", id);
}
```

- Um service por agregado, com a interface `I<Entidade>Service` em `Application/Interfaces`.
- **Recebe e devolve DTOs.** Entidade nunca sai do service.
- **Normaliza a entrada** antes de usar: `Trim()`, e-mail em minúsculas, CNPJ sem pontuação, opcional em branco vira `null`.
- **Checa duplicidade antes de gravar** e lança `ConflictException` com uma mensagem que cita o valor.
  O índice único do banco continua sendo a garantia: numa corrida, o `UnitOfWork` converte a violação em
  `DuplicateKeyException` (409).
- Busca por id num `BuscarAsync` privado que lança `NotFoundException`.
- Grava com **um `CommitAsync` por caso de uso**. Várias gravações que precisam andar juntas vão em
  `unitOfWork.ExecuteInTransactionAsync(...)`.
- Não faz validação de formato (isso é do validator) e não conhece HTTP.

## DTOs

```csharp
/// <summary>O CNPJ pode vir com ou sem pontuação; a empresa nasce ativa.</summary>
public record CriarEmpresaRequest(string RazaoSocial, string? NomeFantasia, string Cnpj);

/// <summary>CNPJ não é alterável; ativar/inativar têm endpoints próprios.</summary>
public record AtualizarEmpresaRequest(string RazaoSocial, string? NomeFantasia);

public record EmpresaResponse(Guid Id, string RazaoSocial, string? NomeFantasia, string Cnpj, bool Ativo, DateTime CriadoEm, DateTime? AtualizadoEm)
{
    public static EmpresaResponse DeEntidade(Empresa empresa) => new(...);
}
```

- `record` posicional, uma pasta por entidade: `DTOs/<Entidade>sDtos/`.
- Nomes: `Criar<Entidade>Request`, `Atualizar<Entidade>Request`, `<Entidade>Response`.
- O `Response` tem o mapeamento `static DeEntidade(...)`. Sem AutoMapper.
- **Response nunca expõe dado sensível** (senha, hash) nem campos internos (`Excluido`).
- Status (ativar/inativar) não vai no `Atualizar...Request`: tem endpoint próprio.

## Validators

```csharp
public class CriarUsuarioRequestValidator : AbstractValidator<CriarUsuarioRequest>
{
    public CriarUsuarioRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Senha)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Za-z]").WithMessage("A senha deve conter pelo menos uma letra.")
            .Matches("[0-9]").WithMessage("A senha deve conter pelo menos um número.");
    }
}
```

- Um validator por request, em `Validators/<Entidade>sValidators/`. São **registrados automaticamente** e executados
  pelo `ValidationFilter`. Não chame validator na mão.
- As mensagens padrão já saem em pt-BR. Escreva `WithMessage` quando a padrão não disser como corrigir.
- **Limites iguais aos do mapeamento EF** (ex.: `MaximumLength(150)` ↔ `HasMaxLength(150)`) e iguais ao schema zod do frontend.
- Validator cobre formato. Regra que depende do banco (duplicidade, existência) fica no service.

## Controllers

```csharp
[ApiController]
[Route("empresas")]
public class EmpresasController(IEmpresaService empresaService) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<EmpresaResponse>> Listar(
        [Range(1, int.MaxValue, ErrorMessage = "A página deve ser maior ou igual a 1.")] int page = 1,
        [Range(1, 100, ErrorMessage = "O tamanho da página deve estar entre 1 e 100.")] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => empresaService.ListarAsync(page, pageSize, cancellationToken);

    [HttpPost]
    [ProducesResponseType<EmpresaResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(CriarEmpresaRequest request, CancellationToken cancellationToken)
    {
        var empresa = await empresaService.CriarAsync(request, cancellationToken);

        return CreatedAtAction(nameof(Obter), new { id = empresa.Id }, empresa);
    }
}
```

- **Controller fino**: cada action faz uma chamada ao service. Sem regra, sem `try/catch`, sem acesso a repositório.
- Rota em português, plural e minúsculas (`/empresas`, `/usuarios`). Id sempre com restrição `{id:guid}`.
- Todas as actions recebem `CancellationToken`.
- Endpoints padrão de um cadastro:

| Ação | Verbo e rota | Resposta |
| --- | --- | --- |
| Listar | `GET /empresas?page=1&pageSize=20` | 200 com `PagedResult` |
| Obter | `GET /empresas/{id}` | 200 ou 404 |
| Criar | `POST /empresas` | **201** com `Location` e o recurso |
| Atualizar | `PUT /empresas/{id}` | 200 com o recurso |
| Ativar / Inativar | `PATCH /empresas/{id}/ativar` e `/inativar` | **204** |

## Exceções

Use as exceções de `Domain/Exceptions` para erro esperado. O `ExceptionFilter` converte em ProblemDetails
(tabela em [Arquitetura → Erros](arquitetura.md#erros)).

- `NotFoundException("Empresa", id)` → 404
- `ConflictException("Já existe uma empresa com o CNPJ 11.222.333/0001-81.")` → 409
- `BusinessRuleException("...")` → 422

A mensagem vai para a tela: frase completa, em português, sem dado sensível e sem detalhe técnico.
Erro de programação (argumento inválido, estado impossível) usa as exceções padrão do .NET e vira 500.

## Injeção de dependência

**Regra fixa: todo tipo novo é registrado no módulo da sua camada, na mesma mudança.**

- Application → `ApplicationModule.AddApplicationModule()` (`AddServices`). Validators entram sozinhos.
- Infrastructure → `InfraModule.AddInfraModule(configuration)` (`AddDatabase`, `AddRepositories`, `AddSecurity`, ...).
- O `Program.cs` só chama os dois módulos. Nunca registre service de camada direto nele.
- Tempo de vida: `Scoped` por padrão (tudo que usa `AppDbContext` ou contexto da requisição).
  `Singleton` só para classe sem estado, com um comentário dizendo isso (ex.: `PasswordHasher`).

## EF Core e banco

```csharp
public class EmpresaConfig : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> builder)
    {
        builder.ToTable("empresas");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RazaoSocial).HasMaxLength(150).IsRequired();
        builder.Property(e => e.NomeFantasia).HasMaxLength(150);
        // Sempre normalizado (sem pontuação, maiúsculo). Único só entre as não excluídas.
        builder.Property(e => e.Cnpj).HasMaxLength(Cnpj.Length).IsFixedLength().IsRequired();
        builder.HasIndex(e => e.Cnpj).IsUnique().HasFilter("excluido = false");
        builder.Property(e => e.Ativo).IsRequired();
    }
}
```

- Um `IEntityTypeConfiguration<T>` por entidade em `DatabaseConfigs/`. Nada de atributos de mapeamento na entidade.
- `ToTable` no plural em português. Colunas em `snake_case` pela convenção (não nomeie coluna na mão).
- **Todo texto tem `HasMaxLength`**, igual ao validator.
- **Índice único sempre com `HasFilter("excluido = false")`**, por causa da exclusão lógica.

### Migrations

```bash
cd backend
dotnet tool restore
dotnet ef migrations add AddFiliais -p src/FluxusManager.Infrastructure -s src/FluxusManager.API
```

- Nome em inglês, `Add<Coisa>` / `Alter<Coisa>`, descrevendo a mudança.
- **Tabela nova liga a auditoria logo depois do `CreateTable`**, com as colunas sensíveis como ignoradas:
  `migrationBuilder.EnableAuditTracking("usuarios", "senha_hash");`
- As migrations rodam sozinhas na subida da API (`Database:MigrateOnStartup`).
- O teste `Migrations_AplicamNumBancoVazio_ESemAlteracaoPendenteNoModelo` falha se o modelo mudou sem migration.
- Não edite migration que já está na `dev`: crie outra.

## Logs

- **Serilog estruturado.** Mensagem com template e propriedades, nunca interpolação:
  `logger.LogInformation("Aplicando {Quantidade} migration(s): {Migrations}", ...)`.
- Cada requisição já gera um log (método, caminho, status e duração) com correlation id, tenant e usuário.
  Não logue entrada e saída de método.
- **Nunca logue** corpo de requisição, query string, senha, hash, token ou dado pessoal.
- `LogError` com a exceção para falha inesperada; o `ExceptionFilter` já faz isso nos controllers.
- Rotina em segundo plano não pode derrubar a API: capture a exceção, logue e siga (ver `AuditPartitionMaintenance`).

## Testes

```bash
cd backend
dotnet test
# sem Docker no WSL: use o Postgres do docker compose
export FLUXUS_TEST_POSTGRES="Host=localhost;Port=5432;Username=fluxus;Password=fluxus"
```

| Projeto | Para quê | Como |
| --- | --- | --- |
| `FluxusManager.UnitTests` | Domain, services, regras do `AppDbContext` | xUnit + **SQLite em memória**, com as classes reais (sem biblioteca de mock) |
| `FluxusManager.IntegrationTests` | Endpoints, filtros, migrations, auditoria | `WebApplicationFactory` + **PostgreSQL de verdade** (Testcontainers ou `FLUXUS_TEST_POSTGRES`), um banco por teste |

- Uma pasta por feature (`Empresas/`, `Usuarios/`), igual ao código.
- Nome do teste em português: `Metodo_Cenario_Resultado`, ex.: `Criar_ComCnpjJaCadastrado_EmOutroFormato_Falha`,
  `Post_ComCnpjDuplicado_Retorna409`.
- Cada cadastro novo tem, no mínimo:
  - teste do service para criar (com normalização), duplicado, atualizar, ativar/inativar e inexistente;
  - teste de endpoint para 201 com `Location`, 400 com erros por campo, 409 e 404.
- Teste de integração que precisa de banco usa `[Collection(PostgresCollection.Name)]` e `ApiComBancoFactory`.

## Checklist: novo cadastro no backend

1. Entidade em `Domain/Entities` herdando `BaseEntity` (e `ITenantEntity`, se for de uma empresa).
2. `I<Entidade>Repository` (só se tiver consulta própria) e o repositório herdando `RepositoryBase<T>`.
3. `<Entidade>Config` em `DatabaseConfigs`, com tamanhos e índices únicos filtrados.
4. Migration com `EnableAuditTracking` logo depois do `CreateTable`.
5. DTOs (`Criar`, `Atualizar`, `Response` com `DeEntidade`) e validators.
6. `I<Entidade>Service` + service.
7. Registro no `ApplicationModule` e no `InfraModule`.
8. Controller com os endpoints padrão.
9. Testes unitários e de integração.
10. `dotnet format`, `dotnet build` e `dotnet test` verdes.
