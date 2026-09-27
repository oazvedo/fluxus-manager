# Arquitetura

Sistema de gestão empresarial **multi-tenant**: cada empresa cliente é um tenant e só enxerga os próprios dados.

| Parte | Tecnologia |
| --- | --- |
| Backend | .NET 10, ASP.NET Core (controllers), EF Core 10, FluentValidation, Serilog |
| Banco | PostgreSQL 18 (nomes em `snake_case`), auditoria por trigger |
| Frontend | React 19 + TypeScript, Vite, React Router, TanStack Query, react-hook-form + zod, Tailwind 4, shadcn/ui (Base UI) |
| Execução | Docker Compose: `postgres`, `api` e `web` (nginx, que repassa `/api` para a API) |

## Camadas do backend

```
backend/src/
├── FluxusManager.Domain           Entidades, regras puras, interfaces de repositório, exceções de negócio
├── FluxusManager.Application      Casos de uso (services), DTOs, validators, contratos de infraestrutura
├── FluxusManager.Infrastructure   EF Core, repositórios, UnitOfWork, migrations, segurança
└── FluxusManager.API              Controllers, filtros, logging, Program.cs
```

Dependências permitidas (a seta aponta para quem é referenciado):

```
API ──► Application ──► Domain
 │                        ▲
 └────► Infrastructure ───┘ (e Application, para implementar as interfaces de lá)
```

- **Domain não depende de nada.** Sem EF, sem ASP.NET, sem pacotes.
- **Application depende só do Domain.** Define interfaces para o que precisa de fora (`IPasswordHasher`,
  `ITenantContext`) e a Infrastructure implementa.
- **Infrastructure** implementa as interfaces do Domain (repositórios, `IUnitOfWork`) e da Application.
- **API** só compõe: recebe HTTP, chama um service e devolve o resultado. Não tem regra de negócio.
- **Program.cs só chama os módulos** `AddApplicationModule()` e `AddInfraModule(configuration)`. Todo tipo novo
  de uma camada é registrado no módulo dela (ver [Backend → Injeção de dependência](backend.md#injeção-de-dependência)).

## Fluxo de uma requisição

```
nginx (/api) ─► CorrelationIdMiddleware ─► log da requisição ─► roteamento ─► filtros ─► controller ─► service ─► repositório ─► UnitOfWork ─► PostgreSQL
```

1. **CorrelationIdMiddleware**: usa o `X-Correlation-Id` recebido (se válido) ou gera um. Ele volta no header da
   resposta, entra em todos os logs e vai no corpo de todo erro (`correlationId`).
2. **Filtros MVC**, nesta ordem (registrados no `Program.cs`):
   - `TenantFilter`: lê a claim `tenant_id` e define o `ITenantContext`.
   - `AuditFilter`: lê o usuário (claim `sub`) e a tela do frontend (header `X-Frontend-Url`) para o `IAuditContext`.
   - `ValidationFilter`: roda o validator FluentValidation de cada argumento. Com erro, responde 400 e a action não roda.
   - `ExceptionFilter`: converte exceções em `ProblemDetails` (ver [Erros](#erros)).
3. **Controller** chama um método do service.
4. **Service** orquestra: busca pelo repositório, aplica a regra na entidade e chama `IUnitOfWork.CommitAsync`.
5. **UnitOfWork** abre a transação, passa o contexto de auditoria ao PostgreSQL (`set_config`) e grava.

## Multi-tenant

- Entidade que pertence a uma empresa implementa `ITenantEntity` (`TenantId`).
- O `AppDbContext` aplica um **filtro global por tenant** em todas essas entidades: consultas só trazem registros do
  tenant da requisição. Sem tenant definido, não trazem nada.
- Ao incluir, o `TenantId` é **preenchido automaticamente**. Incluir em outro tenant, alterar o tenant de um
  registro ou mexer em registro de outro tenant lança erro.
- Registro de outro tenant se comporta como inexistente: **404**, nunca 403, para não revelar que ele existe.
- `Empresa` e `Usuario` são globais (não são `ITenantEntity`): a empresa é o próprio tenant, e o usuário se liga a
  empresas por vínculo.
- Para ignorar o filtro (casos administrativos, com justificativa): `IgnoreQueryFilters([AppDbContext.TenantFilter])`.

## Exclusão lógica

- Toda entidade herda `BaseEntity`, que tem `Excluido`.
- `repositorio.Remove(entidade)` **não apaga**: o `AppDbContext` transforma o DELETE em UPDATE de `excluido = true`.
- Um filtro global esconde os excluídos de todas as consultas.
- Índices únicos são **filtrados** por `excluido = false`, então o CNPJ ou e-mail de um registro excluído pode ser reaproveitado.
- `ExecuteDelete` e SQL direto passam por fora e apagam de verdade. Não use sem motivo.

"Inativar" é outra coisa: é um status de negócio (`Ativo = false`), visível e reversível na tela.

## Auditoria

Toda alteração em tabela vira uma linha em `audit.change_log`, gravada por **trigger no PostgreSQL**: operação,
valores antigos e novos (JSONB), campos alterados, usuário, tenant, tela do frontend, usuário do banco e transação.

- A migration que cria uma tabela **precisa** chamar `migrationBuilder.EnableAuditTracking("tabela")` logo depois do
  `CreateTable`. O teste `TodaTabelaDoSistema_TemOTriggerDeAuditoria` falha se faltar.
- Colunas sensíveis entram como ignoradas: `EnableAuditTracking("usuarios", "senha_hash")`. A troca aparece em
  `changed_fields`, mas o valor nunca é gravado.
- O frontend manda a tela atual no header `X-Frontend-Url` em toda requisição (interceptor do `http`).
- A tabela é particionada por mês; a API cria as partições futuras todo dia (`AuditPartitionMaintenance`).

## Erros

A API sempre responde erro como **ProblemDetails (RFC 9457)**, com `correlationId`:

| Situação | Como surge | Status |
| --- | --- | --- |
| Dados inválidos | Validator FluentValidation ou `[Range]`, JSON mal formado | **400**, com `errors` por campo em camelCase (`"razaoSocial": ["..."]`) |
| Não encontrado (ou de outro tenant) | `throw new NotFoundException("Empresa", id)` | **404** |
| Duplicado / conflito com o estado atual | `throw new ConflictException("...")`; índice único violado vira `DuplicateKeyException` | **409** |
| Regra de negócio violada | `throw new BusinessRuleException("...")` | **422** |
| Inesperado | Qualquer outra exceção | **500**, com mensagem genérica e o código de correlação (o detalhe só aparece em Development) |

A mensagem de `DomainException` vai para o usuário: clara, em português e sem dado sensível.
O frontend leva os erros 400 e 409 para o campo certo do formulário (ver [Frontend → Formulários](frontend.md#formulários)).

## Frontend

Organizado **por feature** (`src/features/<feature>`), com uma base compartilhada (`src/shared`) e o núcleo da
aplicação (`src/app`, `src/core`). Em produção, o nginx serve o build e repassa `/api/*` para a API, que roda com
`PathBase=/api`. Em desenvolvimento, o Vite faz proxy de `/api` para `http://localhost:5250`, tirando o prefixo.
Detalhes em [Frontend](frontend.md).
