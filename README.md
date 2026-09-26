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
