# Fluxo de trabalho

## Branches

| Branch | Papel | Regras |
| --- | --- | --- |
| `main` | Produção (é o que roda no servidor) | **Nunca recebe commit direto.** Só PR vindo de `dev`, `release/*` ou `hotfix/*`, com CI verde |
| `dev` | Integração (branch padrão) | Aceita commit direto; recebe os PRs das features |
| `feat`, `bugfix`, `hotfix` | Trabalho de uma issue | `<tipo>/<issue>-<descricao>`: `feat/49-telas-usuarios`, `bugfix/42-token-expirado` |
| `chore`, `docs`, `refactor`, `test`, `ci`, `release` | Demais trabalhos | `<tipo>/<descricao>`, número da issue opcional: `docs/regras-do-projeto`, `release/1.0.0` |

Tudo em minúsculas, palavras separadas por hífen. Branch de trabalho sai da `dev` e volta para a `dev` por PR.

## Começando uma issue

```bash
scripts/start-issue.sh <issue> <descricao> [tipo]   # tipo padrão: feat
# ex.: scripts/start-issue.sh 13 cadastro-filiais  →  feat/13-cadastro-filiais
```

O script cria a branch a partir da `dev` vinculada à issue (seção *Development*), faz o checkout, atribui a issue a
você e move o card do board para **In Progress**.

## Commits

```
<tipo>(escopo opcional): descrição
```

- **Tipos:** `feat`, `fix`, `bugfix`, `hotfix`, `chore`, `docs`, `refactor`, `test`, `ci`, `perf`, `style`, `build`, `release`, `revert`.
- **Escopo:** a área, em minúsculas e em português: `empresas`, `usuarios`, `auditoria`, `persistencia`.
- **Descrição** em português, minúscula, no presente e dizendo o que muda para quem usa ou mantém:
  `feat(empresas): cadastro de empresas (CRUD) com validação de CNPJ numérico e alfanumérico`,
  `fix(persistencia): cadastro simultâneo com CNPJ repetido responde 409 em vez de 500`.
- Em branch com número de issue, **todo commit referencia a issue**. O hook `prepare-commit-msg` acrescenta
  `Refs #<issue>` sozinho.
- **Sem atribuição de ferramentas** (`Co-Authored-By` de IA, "Generated with ..."): o autor é quem abre o PR.
- Merge e revert gerados pelo git são aceitos como estão.

Ative os hooks uma vez por clone:

```bash
git config core.hooksPath .githooks
```

Os hooks bloqueiam commit na `main`, branch fora do padrão, mensagem fora do padrão e commit sem a issue.
As regras ficam em `.githooks/lib/conventions.sh`, as mesmas que o CI usa.

## Pull requests

- **Título** no mesmo formato do commit: `feat(usuarios): tela de usuários`.
- **Descrição** começa com `Closes #<issue>` (obrigatório em branch com issue; a issue fecha no merge).
  Depois: o que muda e como funciona, decisões tomadas, como foi validado e o que ficou de fora.
- **Base:** `dev`. Se a issue depende de outro PR ainda aberto, abra um PR empilhado (base = branch da dependência);
  quando ela for mergeada, o GitHub apaga a branch e troca a base para a `dev`.
- **Um PR por issue**, com o código, os testes e a documentação juntos. Se o PR muda um padrão, atualize o documento
  correspondente em `docs/` no mesmo PR.
- **Antes de abrir**, valide localmente:
  - backend: `dotnet format`, `dotnet build` e `dotnet test`;
  - frontend: `npm run lint`, `npm test` e `npm run build`; autenticação também com `npm run test:e2e`;
  - tela: teste no navegador com a API local.

## CI

Em todo PR para `dev` ou `main` e em todo push nelas:

| Workflow / job | O que faz |
| --- | --- |
| **Conventions** | Valida o nome da branch, o título do PR, o `Closes #n`, cada commit e a origem de PR para a `main` |
| **Backend (.NET)** | `dotnet restore`, `dotnet format --verify-no-changes`, build Release e todos os testes |
| **Frontend (React)** | `npm ci`, lint, testes unitários, build e login no Chromium com API/PostgreSQL reais |
| **Docker (imagens)** | Build das imagens da API e do frontend (as mesmas do deploy) |
| **Deploy (AWS)** | Só em push na `main`, depois dos outros jobs verdes, se `DEPLOY_EC2_ENABLED=true` |

A `main` só aceita merge com Backend, Frontend e Conventions verdes.

## Ambiente local

```bash
cp .env.example .env
docker compose up -d                           # PostgreSQL 18 em localhost:5432 e Mailpit em localhost:8025

cd backend && dotnet tool restore
dotnet run --project src/FluxusManager.API     # http://localhost:5250 (Swagger em /swagger)

cd frontend && npm install && npm run dev      # http://localhost:5173, com proxy de /api para a API
```

- .NET SDK 10 e Node 24.
- As migrations rodam sozinhas quando a API sobe.
- Para entrar no frontend local, configure a senha do administrador antes de subir a API:
  `dotnet user-secrets set 'DevelopmentSeed:AdminPassword' '<sua-senha-local-de-12-ou-mais-caracteres>' --project backend/src/FluxusManager.API`.
  O seed cria `admin@fluxus.local` vinculado à empresa de desenvolvimento com todas as permissões.
  Veja [Administrador inicial](backend.md#administrador-inicial-de-desenvolvimento).
- E-mails enviados pela API local (convites, recuperação de senha) não saem da máquina: abra o Mailpit em
  http://localhost:8025. Veja [E-mail](backend.md#e-mail).
- Testes de integração sem Docker disponível: `FLUXUS_TEST_POSTGRES="Host=localhost;Port=5432;Username=fluxus;Password=fluxus"`.
- **No WSL** (sem a integração do Docker Desktop): rode o `docker compose` pelo Windows. O WSL acessa o Postgres em `localhost:5432`.

## Release e deploy

1. Abra um PR `dev` → `main` com título `release: ...` e espere o CI verde.
2. Aprove e faça o merge.
3. O CI roda na `main` e, verde, o job de deploy entra no servidor por SSH, roda `deploy/update.sh` (sincroniza com
   a `main` e reconstrói os containers) e confere o `/api/health`.

Merge na `dev` não vai para o servidor. Correção urgente em produção sai de `hotfix/<issue>-<descricao>` direto para
a `main` e volta para a `dev`. O passo a passo do servidor está no [`README`](../README.md#deploy-em-servidor-ec2).
Hoje o deploy está **pausado** (`DEPLOY_EC2_ENABLED=false`, sem servidor na AWS).

## Organização das issues

- Board: projeto **FluxusManager** no GitHub (colunas de status, a issue vai para *In Progress* ao começar).
- Milestones por fase (`Fase 1`, `Fase 2 — Cadastros e acesso`, ...). O que ficou para depois vai para milestones
  "Futuro" (ex.: `Futuro — Autenticação (Cognito)`).
- Labels por área: `backend`, `frontend`, `auth`, `devops`, e pelo módulo (`empresa`, `filial`, `vinculo`...).
- Título da issue com a área entre colchetes: `[Front] Telas de usuários`, `[Filial] Cadastro de filiais (CRUD)`.
