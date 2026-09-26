# fluxus-manager

Sistema de gestão empresarial multi-tenant.

## Stack

- **Backend:** .NET 10 (ASP.NET Core + EF Core), em camadas — API, Application, Domain, Infrastructure
- **Banco:** PostgreSQL 18 (Docker)
- **Frontend:** React + TypeScript (Vite), Tailwind e shadcn/ui, organizado por feature

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

## Fluxo de branches e commits

- **`main`**: produção. Nunca recebe commit direto — só PR vindo da `dev`, `release/*` ou `hotfix/*`, com CI verde.
- **`dev`**: integração. Aceita commit direto.
- **Demais branches**: `<tipo>/<descricao>` em minúsculas — tipos `feat`, `bugfix`, `hotfix`, `chore`, `docs`, `refactor`, `test`, `ci`, `release`.
  Ex.: `feat/1-multi-tenant`, `bugfix/login-token-expirado`.
- **Commits** (e títulos de PR): `<tipo>(escopo opcional): descrição` — tipos `feat`, `fix`, `bugfix`, `hotfix`, `chore`, `docs`, `refactor`, `test`, `ci`, `perf`, `style`, `build`, `release`, `revert`.
  Ex.: `feat(empresa): cadastro de empresas`.

As regras ficam em `.githooks/lib/conventions.sh` e são validadas no GitHub (workflow **Conventions**) e localmente pelos hooks. Ative os hooks uma vez por clone:

```bash
git config core.hooksPath .githooks
```
