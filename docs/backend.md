# Backend

.NET 10, com `Nullable` e `ImplicitUsings` ligados em todos os projetos. As camadas e o fluxo da requisição estão em
[Arquitetura](arquitetura.md). Aqui fica **como escrever cada peça**, com os modelos de Empresas e Usuários.

## Administrador inicial de desenvolvimento

O startup executa `DevelopmentSeeder` depois das migrations, exclusivamente em `Development` e com
`DevelopmentSeed:Enabled=true` (padrão de `appsettings.Development.json`). Configure antes do primeiro uso:

```bash
dotnet user-secrets set 'DevelopmentSeed:AdminPassword' '<senha-local-com-ao-menos-12-caracteres>' --project backend/src/FluxusManager.API
```

O seed cria `admin@fluxus.local`, a empresa **Fluxus Desenvolvimento Ltda** (CNPJ fictício `47.986.213/0001-06`)
e o vínculo `Administrador`, que recebe todas as permissões do catálogo. A senha fica em user-secrets, nunca no
repositório; apenas o hash vai ao banco, excluído dos valores de auditoria. Também aceita a variável
`DevelopmentSeed__AdminPassword`. Sem senha, avisa no log e não cria os registros.

Uma transação com bloqueio garante a criação conjunta e serializa startups concorrentes. O ID reservado do usuário
identifica a execução anterior mesmo se o e-mail mudar. Reiniciar não redefine senha, perfil, status ou vínculo,
nem recria um administrador excluído. Conflitos de e-mail/CNPJ interrompem o bootstrap sem promover usuários existentes.
Alterar o secret depois da criação não altera a senha já persistida.

Para desabilitar, configure `DevelopmentSeed:Enabled=false`. Em produção o seed é ignorado mesmo com a flag ligada.
Testes desabilitam o seed por padrão; os testes específicos exercitam o bootstrap com autenticação real.

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

### Regra de raiz do CNPJ de filial

A compatibilidade entre os oito primeiros caracteres do CNPJ da filial e da empresa é controlada pela configuração
`Filiais:ValidarRaizCnpjDaEmpresa`. O padrão (`appsettings.json`) é `true`; `appsettings.Development.json` e os testes
desativam a regra para permitir cadastros com CNPJs de teste. Em produção, mantenha `true`. A validação de formato e
dígitos verificadores do CNPJ continua ativa em todos os ambientes.

Para alternar por variável de ambiente, use `Filiais__ValidarRaizCnpjDaEmpresa=true` ou `false`.

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

## Autenticação e permissões

As rotas de negócio exigem access token JWT. `POST /auth/login` recebe apenas `{ email, senha }` e emite um token de
15 minutos para o primeiro vínculo ativo com empresa ativa; envie-o como `Authorization: Bearer <token>`. A escolha
de empresa não faz parte do payload de login. `POST /auth/switch-tenant` recebe `empresaId`, verifica se o usuário e
o vínculo estão ativos e emite um novo token com o tenant selecionado. Tokens de acesso antigos expiram normalmente.

O token contém `sub`, `email`, `tenant_id`, `role` e uma claim `permissions` por permissão concedida. A API valida
assinatura, emissor, audiência e expiração. As rotas declaram permissões por `[HasPermission("empresas.editar")]`;
políticas e o fallback exigem autenticação. Cada empresa tem perfis próprios: novas empresas recebem `Administrador`
com todas as permissões e `Consulta` com acesso de leitura. O catálogo de códigos é definido pela aplicação e pode ser
consultado em `GET /perfis/permissoes`. Os vínculos guardam `perfilId`, em vez de um nome livre.

`GET /perfis` e `GET /perfis/{id}` consultam os perfis do tenant selecionado. `POST /perfis` cria, `PUT /perfis/{id}`
atualiza nome, descrição e permissões, `PATCH /perfis/{id}/ativar|inativar` altera o status e `DELETE /perfis/{id}`
exclui logicamente. `GET` exige `perfis.visualizar`; alterações exigem `perfis.editar`. Nome não diferencia maiúsculas
de minúsculas e as permissões precisam pertencer ao catálogo. Perfil com vínculo ativo precisa ser reatribuído antes
de inativar ou excluir. O token atual preserva as claims emitidas até expirar; o refresh recalcula perfil e permissões.

A migration cria os perfis padrão em empresas existentes, associa os vínculos antigos e conserva nomes legados com
acesso vazio quando não eram reconhecidos pelo mapeamento anterior. A mesma empresa é exigida no perfil e no vínculo;
as consultas e chaves estrangeiras compostas isolam permissões entre tenants.

Configure `Jwt:SigningKey` por secret, com pelo menos 32 bytes. Em Docker, gere uma chave aleatória e defina
`JWT_SIGNING_KEY` em `deploy/.env`; nunca reutilize a chave de desenvolvimento ou a inclua no repositório. O issuer e a
audience padrão são `FluxusManager`, e a duração pode ser alterada por `Jwt__AccessTokenMinutes` (1–60 minutos).

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

## Refresh tokens

O login retorna também `refreshToken` e `refreshTokenExpiresAt`. Para renovar, envie
`POST /auth/refresh` com `{ "refreshToken": "<credencial>" }`. A resposta contém um novo access token e um novo refresh
token; substitua a credencial anterior e serialize as renovações no cliente. O refresh é uma credencial independente:
não exige access token válido. A API revalida usuário, empresa e vínculo e recalcula as permissões a cada renovação.

Depois de `switch-tenant`, informe também `empresaId` na próxima renovação para manter a empresa selecionada.
Esse vínculo é validado novamente. O sucessor guarda a nova empresa, dispensando repeti-la nas renovações seguintes.
Uma empresa sem vínculo ativo invalida a renovação e revoga a família.

Cada login cria uma família independente. A rotação consome o token anterior e conserva a ligação com seu sucessor.
Reutilizar um token consumido responde 401 e revoga toda a família, incluindo o token emitido pela outra requisição em
caso de concorrência. O bloqueio transacional por família no PostgreSQL também serializa logout e rotação entre
instâncias da API. O commit da revogação ocorre antes da resposta 401.

`POST /auth/logout` recebe `{ "refreshToken": "<credencial>" }` e revoga a família, inclusive se receber um token
anterior já substituído. Responde 204 também para uma credencial desconhecida ou já revogada. Outros logins permanecem
ativos. Access tokens JWT emitidos anteriormente continuam válidos até expirar (15 minutos por padrão).

A validade da família é absoluta: sete dias contados do login, sem extensão ao rotacionar. Configure por
`RefreshTokens:DuracaoDias` (1–90). A limpeza diária remove fisicamente as famílias vencidas; pode ser desligada com
`RefreshTokens:LimpezaHabilitada=false`. Tokens consumidos são mantidos até esse prazo para permitir a detecção de
reuso. Esta limpeza de credenciais efêmeras é uma exceção à exclusão lógica dos cadastros.

As credenciais são 256 bits aleatórios e somente seu hash SHA-256 fica em `refresh_tokens`. A migration habilita
auditoria excluindo `token_hash` dos valores registrados. Respostas de autenticação usam `Cache-Control: no-store`.
A política de rotação e revogação segue a [seção 4.14 da RFC 9700](https://www.rfc-editor.org/rfc/rfc9700.html#section-4.14).

## Proteção contra força bruta

- **Rate limiting** (`RateLimitingExtensions`): toda action de controller com `[AllowAnonymous]` (hoje login e os
  convites públicos) é limitada automaticamente, numa janela fixa de 1 minuto por IP e action
  (`RateLimiting:RequisicoesPorMinuto`, padrão 20). Rota pública nova já nasce protegida; health check e OpenAPI
  ficam de fora. Acima do limite: **429** em ProblemDetails, com `Retry-After` e uma mensagem dizendo quanto aguardar.
  IPv4 mapeado em IPv6 conta como IPv4, e IPv6 é agrupado por /64.
- **Refresh e logout** têm `[DisableRateLimiting]`: a credencial (256 bits) não é adivinhável, e um 429 ali
  derrubaria a sessão de quem divide o IP (NAT de escritório) ou deixaria de revogar o token no logout. O cliente
  também não descarta a sessão se receber 429 no refresh.
- **IP real atrás do nginx:** `Proxy:Confiavel=true` (ligado no `deploy/docker-compose.yml`) faz a API usar o último
  salto do `X-Forwarded-For`, que é o endereço visto pelo nginx. Só ligue com a API atrás do proxy: sem ele, o cliente
  poderia forjar o cabeçalho.
- **Bloqueio do usuário:** `Login:MaxTentativas` (padrão 5) senhas erradas dentro de `Login:BloqueioMinutos`
  (padrão 15) bloqueiam o login por esse mesmo prazo, mesmo com a senha certa. Falhas mais antigas que a janela não
  somam. A contagem é um único UPDATE atômico no PostgreSQL (`IUsuarioRepository.RegistrarFalhaLoginAsync`), então
  tentativas simultâneas não se perdem. Login concluído zera tudo; o administrador desbloqueia em
  `PATCH /usuarios/{id}/desbloquear` (`usuarios.editar`), e `GET /usuarios` mostra `bloqueadoAte`. Os campos ficam em
  `usuarios`, e cada falha gera uma linha de auditoria (trilha de tentativas).
- **Resposta genérica:** e-mail inexistente, senha errada, usuário inativo ou bloqueado e falta de vínculo respondem o
  mesmo 401. Os caminhos sem usuário válido também pagam o custo do hash (`IPasswordHasher.VerificarSemUsuario`),
  que domina o tempo de resposta; resta só a pequena diferença do UPDATE na senha errada de um usuário real.
- Quem conhece um e-mail consegue mantê-lo bloqueado errando a senha de tempos em tempos: é o custo do bloqueio por
  conta. O rate limiting por IP limita o ritmo, e o administrador pode desbloquear.

## Recuperação e troca de senha

| Ação | Verbo e rota | Acesso / resposta |
| --- | --- | --- |
| Solicitar recuperação | `POST /auth/forgot-password` com `{ email }` | Público; sempre 200 com a mesma mensagem |
| Redefinir pelo link | `POST /auth/reset-password` com `{ token, novaSenha }` | Público; 204; link inválido ou expirado: 422 |
| Trocar senha | `POST /auth/change-password` com `{ senhaAtual, novaSenha }` | Autenticado; 204; senha atual incorreta: 422 |

- O link vai para `{Frontend:Url}/redefinir-senha?token=<token>` e vence após `RecuperacaoSenha:ValidadeMinutos`
  (padrão 60, entre 5 e 1440). O token aleatório só é enviado por e-mail; a tabela `password_reset_tokens` guarda
  apenas o SHA-256. Tokens vencidos ou já usados não podem redefinir a senha.
- Pedido para e-mail inexistente ou usuário inativo tem a mesma resposta pública; falha SMTP também não altera a
  resposta. A entrega pode ser conferida no Mailpit local.
- Redefinir ou trocar a senha limpa o bloqueio de login e revoga os refresh tokens ativos do usuário. A troca exige
  a senha atual. A redefinição confirma e consome o token dentro da mesma transação.
- Ambas usam a política de senha existente: ao menos uma letra e um número.

## Convites

Um administrador convida alguém por e-mail para a empresa selecionada, com um perfil ativo dela. As rotas usam as
permissões de vínculo: `usuarios-empresas.visualizar` para consultar e `usuarios-empresas.editar` para alterar.

| Ação | Verbo e rota | Resposta |
| --- | --- | --- |
| Listar / obter | `GET /convites?page=1&pageSize=20`, `GET /convites/{id}` | 200 |
| Convidar | `POST /convites` com `{ email, perfilId }` | **201**; envia o e-mail |
| Reenviar | `POST /convites/{id}/reenviar` | 200; novo link e novo prazo, o anterior deixa de valer |
| Cancelar | `PATCH /convites/{id}/cancelar` | **204**; aceito não pode ser cancelado (422) |
| Consultar (público) | `POST /convites/consultar` com `{ token }` | 200 com e-mail, empresa, perfil e `usuarioExistente` |
| Aceitar (público) | `POST /convites/aceitar` com `{ token, nome?, senha? }` | **204** |

- O link enviado é `{Frontend:Url}/convites/aceitar?token=<token>`. O token (256 bits, `SecretToken`) só existe no
  e-mail; o banco guarda o SHA-256, excluído da auditoria. As rotas públicas recebem o token no corpo e respondem
  com `Cache-Control: no-store`.
- O status gravado é `Pendente`, `Aceito` ou `Cancelado`; a API devolve `Expirado` para pendente vencido.
  O prazo é `Convites:ValidadeHoras` (padrão 72, entre 1 e 720), contado do envio ou do último reenvio.
- Um convite pendente por e-mail em cada empresa (índice único). Convidar de novo um e-mail com convite vencido
  cancela o anterior; com convite no prazo responde 409 (use reenviar). E-mail que já tem vínculo com a empresa: 409.
- Convidar ou reenviar para usuário inativo (422), com vínculo ativo (409) ou inativo (409, reative o vínculo) é
  recusado, assim como convidar numa empresa inativa: o link enviado sempre pode ser aceito.
- O e-mail é montado antes e enviado dentro da transação: se o envio falhar, o convite não é gravado.
- Aceite, cancelamento e reenvio concorrentes: o convite usa o `xmin` do PostgreSQL como token de concorrência, e o
  `UnitOfWork` converte a gravação perdida em 409. Um perfil com convite pendente não pode ser excluído (422).
- **Aceite:** se já existe usuário com o e-mail, só cria o vínculo (nome e senha são ignorados; usuário inativo
  recebe 422). Senão, `nome` e `senha` são obrigatórios e o usuário é criado junto com o vínculo. O aceite confere
  empresa ativa e perfil ainda ativo. Quem tem o link comprova o acesso ao e-mail, por isso o aceite não pede login.
- As rotas públicas ignoram um `tenant_id` de token enviado junto: o `TenantFilter` não atua em `[AllowAnonymous]`,
  e o service passa a operar na empresa do convite. A auditoria registra o usuário que aceitou.
- `Frontend:Url` é obrigatório (URL absoluta HTTP(S) do frontend). Em Development é `http://localhost:5173`; no
  deploy vem de `PUBLIC_URL` em `deploy/.env`. Nunca é montado a partir do `Host` da requisição.

## E-mail

Services enviam e-mail por `IEmailSender.EnviarAsync(MensagemEmail)` (`Application/Interfaces`). A implementação
`SmtpEmailSender` (`Infrastructure/Email`, MailKit) abre uma conexão por mensagem e lança exceção se o envio falhar;
quem chama decide se a falha interrompe o caso de uso. O log registra o `Message-ID` e o assunto, nunca o destinatário.

Os textos ficam em `Application/Email/EmailTemplates`, um método por e-mail, gerando HTML e texto:

```csharp
var mensagem = EmailTemplates.Convite(email, empresa.RazaoSocial, link, TimeSpan.FromHours(48));
await emailSender.EnviarAsync(mensagem, cancellationToken);
```

- `Convite(para, nomeEmpresa, link, validade)` e `RecuperacaoSenha(para, nome, link, validade)`.
- O link chega pronto (com o token) e precisa ser uma URL absoluta HTTP(S). Os valores são escapados no HTML.
- E-mail novo: um método em `EmailTemplates` que reutiliza o layout comum, com teste em `UnitTests/Email`.

Configuração na seção `Email`, validada na subida da API:

| Chave | Padrão | Observação |
| --- | --- | --- |
| `Host` | — | Obrigatório. |
| `Port` | `587` | 1–65535. |
| `Seguranca` | `StartTls` | `StartTls` (exige STARTTLS), `SslTls` (TLS desde a conexão, porta 465) ou `Nenhuma` (só local). |
| `Usuario`, `Senha` | vazio | Sem usuário, envia sem autenticação. A senha vai por secret/variável, nunca no repositório. |
| `RemetenteEmail` | — | Obrigatório. Apenas o endereço, sem nome. |
| `RemetenteNome` | `FluxusManager` | |
| `TimeoutSegundos` | `30` | 1–300. |

Em Development, `appsettings.Development.json` aponta para o **Mailpit** do `docker compose` (SMTP em `localhost:1025`,
sem TLS). Todo e-mail enviado fica na caixa de entrada em http://localhost:8025, e nada sai para a internet.
No deploy, configure `EMAIL_HOST`, `EMAIL_REMETENTE` e, se o servidor exigir, `EMAIL_USUARIO` e `EMAIL_SENHA` em `deploy/.env`.
