# fluxus-manager

Sistema de gestão empresarial multi-tenant.

## Stack

- **Backend:** .NET 10 (ASP.NET Core + EF Core), em camadas — API, Application, Domain, Infrastructure
- **Banco:** PostgreSQL 18 (Docker)
- **Frontend:** React + TypeScript (Vite), Tailwind e shadcn/ui, organizado por feature
- **Deploy:** Docker Compose em um servidor (EC2) — ver [Deploy em servidor](#deploy-em-servidor-ec2)

## Documentação

As regras e os padrões do projeto (arquitetura, backend, frontend, estilo e fluxo de trabalho) estão em [`docs/`](docs/README.md).

## Rodando localmente

```bash
# Banco
cp .env.example .env
docker compose up -d

# Backend (http://localhost:<porta>/swagger)
cd backend
dotnet tool restore
dotnet run --project src/FluxusManager.API

# Frontend
cd frontend
npm install
npm run dev
```

## Testes

```bash
cd backend
dotnet test
```

- `FluxusManager.UnitTests`: Domain, Application e regras do `AppDbContext` (SQLite em memória).
- `FluxusManager.IntegrationTests`: API em memória (`WebApplicationFactory`) com **PostgreSQL de verdade**.
  Cada teste ganha um banco próprio, com as migrations aplicadas.

Os testes de integração sobem um container do PostgreSQL com Testcontainers, então precisam do Docker.
Sem acesso ao Docker (ex.: WSL sem a integração do Docker Desktop), aponte para o banco do `docker compose`:

```bash
export FLUXUS_TEST_POSTGRES="Host=localhost;Port=5432;Username=fluxus;Password=fluxus"
dotnet test
```

Os bancos `fluxus_test_*` criados nesse modo são apagados ao final da execução.

O CI roda format, build e todos os testes em todo PR e em todo push na `dev` e na `main`; a `main` só aceita merge com o check verde.

## Auditoria

Toda alteração nas tabelas vira uma linha em `audit.change_log`, gravada por trigger no PostgreSQL:
operação, valores antigos e novos (JSONB), campos alterados, usuário, tenant e a tela do front (header `X-Frontend-Url`).

**Tabela nova:** na migration que a cria, ligue a auditoria logo depois do `CreateTable`.
O teste `TodaTabelaDoSistema_TemOTriggerDeAuditoria` falha se faltar.

```csharp
migrationBuilder.EnableAuditTracking("empresas");
migrationBuilder.EnableAuditTracking("usuarios", "senha_hash"); // colunas sensíveis: só o nome é gravado
```

- **Excluir** pelo repositório é exclusão lógica: a coluna `excluido` vira `true`, o registro some das consultas e a auditoria registra um UPDATE.
- **Partições mensais:** a API cria as dos próximos 3 meses todo dia. `Auditoria:RetencaoMeses` (padrão `0`, guarda tudo) apaga as mais antigas que isso.

```sql
-- Histórico de um registro
SELECT created_at, operation, changed_fields, application_user, frontend_url
FROM audit.change_log WHERE table_name = 'usuarios' AND row_id = '<id>' ORDER BY id;
```

## Deploy em servidor (EC2)

Banco, API e frontend sobem juntos com Docker Compose; o nginx do frontend repassa `/api` para a API.

**1. Docker na máquina** (uma vez)

```bash
# Amazon Linux 2023
sudo dnf install -y docker git && sudo systemctl enable --now docker
sudo mkdir -p /usr/local/lib/docker/cli-plugins
sudo curl -fsSL "https://github.com/docker/compose/releases/latest/download/docker-compose-linux-$(uname -m)" \
  -o /usr/local/lib/docker/cli-plugins/docker-compose && sudo chmod +x /usr/local/lib/docker/cli-plugins/docker-compose

# Ubuntu
curl -fsSL https://get.docker.com | sudo sh

sudo usermod -aG docker $USER   # saia e entre de novo na sessão
```

No Security Group da EC2, libere a **porta 80** (HTTP). Instâncias com menos de 2 GB de RAM podem
travar no build — crie swap: `sudo fallocate -l 2G /swapfile && sudo chmod 600 /swapfile && sudo mkswap /swapfile && sudo swapon /swapfile`.

**2. Primeira subida**

```bash
cp deploy/.env.example deploy/.env
sed -i "s/troque-esta-senha/$(openssl rand -hex 24)/" deploy/.env
docker compose -f deploy/docker-compose.yml up -d --build
```

Acesse `http://<ip-publico>/` (frontend), `http://<ip-publico>/api/health` e `http://<ip-publico>/api/swagger`.

**3. Atualizar**: automático a cada release (merge `dev` → `main`, abaixo) ou manual com `deploy/update.sh`.

Logs: `docker compose -f deploy/docker-compose.yml logs -f api`

### Deploy automático (release)

O servidor na AWS roda a **`main`**. Fluxo de release:

1. Abra um PR `dev` → `main` (título `release: ...`) e aguarde o CI verde.
2. Aprove e mergeie o PR.
3. O CI roda na `main` e, verde, o workflow **Deploy EC2** entra no servidor por SSH, roda o `deploy/update.sh`
   (sincroniza com a `main` e reconstrói os containers) e confere o `/api/health`.

Merges na `dev` não vão para a AWS.

Configuração (uma vez):

1. No servidor, crie a chave usada pelo GitHub e permita o Docker sem `sudo`:
   ```bash
   ssh-keygen -t ed25519 -f ~/.ssh/github_deploy -N "" -C github-deploy
   cat ~/.ssh/github_deploy.pub >> ~/.ssh/authorized_keys
   sudo usermod -aG docker $USER
   cat ~/.ssh/github_deploy   # copie a chave privada inteira
   ```
2. GitHub → **Settings → Environments → `ec2` → Add environment secret**: `EC2_SSH_KEY` = a chave copiada.
3. Variáveis do environment `ec2`: `EC2_HOST` (IP público — de preferência um Elastic IP) e `EC2_USER` (ex.: `ubuntu`);
   variável de repositório `DEPLOY_EC2_ENABLED=true`.
4. No Security Group, a **porta 22** precisa aceitar conexões do GitHub Actions (IPs variáveis; o acesso é só por chave).

Deploy manual: **Actions → Deploy EC2 → Run workflow** (branch `main`).

## Fluxo de branches e commits

- **`main`**: produção (é o que roda na AWS). Nunca recebe commit direto — só PR vindo da `dev`, `release/*` ou `hotfix/*`, com CI verde.
- **`dev`**: integração. Aceita commit direto.
- **Branches de issue** (`feat`, `bugfix`, `hotfix`): `<tipo>/<issue>-<descricao>` em minúsculas.
  Ex.: `feat/1-multi-tenant`, `bugfix/42-token-expirado`.
- **Demais branches** (`chore`, `docs`, `refactor`, `test`, `ci`, `release`): `<tipo>/<descricao>` — número da issue opcional.
- **Commits** (e títulos de PR): `<tipo>(escopo opcional): descrição` — tipos `feat`, `fix`, `bugfix`, `hotfix`, `chore`, `docs`, `refactor`, `test`, `ci`, `perf`, `style`, `build`, `release`, `revert`.
  Ex.: `feat(empresa): cadastro de empresas`.

### Rastreabilidade issue → branch → commit → PR

- Comece uma issue com `scripts/start-issue.sh <issue> <descricao> [tipo]`: cria a branch vinculada à issue (seção *Development*), faz checkout, atribui a issue a você e move o card para **In Progress**.
- Em branches com número de issue, todo commit precisa referenciar a issue — o hook acrescenta `Refs #<issue>` automaticamente.
- A descrição do PR precisa conter `Closes #<issue>`; ao mergear na `dev` a issue é fechada.

As regras ficam em `.githooks/lib/conventions.sh` e são validadas no GitHub (workflow **Conventions**) e localmente pelos hooks. Ative os hooks uma vez por clone:

```bash
git config core.hooksPath .githooks
```
