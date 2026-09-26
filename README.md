# fluxus-manager

Sistema de gestão empresarial multi-tenant.

## Stack

- **Backend:** .NET 10 (ASP.NET Core + EF Core), em camadas — API, Application, Domain, Infrastructure
- **Banco:** PostgreSQL 18 (Docker)
- **Frontend:** React + TypeScript (Vite), Tailwind e shadcn/ui, organizado por feature
- **Deploy:** Docker Compose em um servidor (EC2) — ver [Deploy em servidor](#deploy-em-servidor-ec2)

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

**3. Atualizar**: automático a cada merge na `dev` (abaixo) ou manual com `deploy/update.sh`.

Logs: `docker compose -f deploy/docker-compose.yml logs -f api`

### Deploy automático

Depois do CI verde na `dev`, o workflow **Deploy EC2** entra no servidor por SSH, roda o `deploy/update.sh`
(sincroniza com a `dev` e reconstrói os containers) e confere o `/api/health`.

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

Deploy manual: **Actions → Deploy EC2 → Run workflow**.

## Fluxo de branches e commits

- **`main`**: produção. Nunca recebe commit direto — só PR vindo da `dev`, `release/*` ou `hotfix/*`, com CI verde.
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
