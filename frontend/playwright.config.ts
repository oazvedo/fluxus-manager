import { defineConfig } from '@playwright/test'

const database = process.env.FLUXUS_E2E_DATABASE
if (!database) throw new Error('Configure FLUXUS_E2E_DATABASE com um banco PostgreSQL exclusivo para os testes.')

export default defineConfig({
  testDir: './e2e',
  workers: 1,
  use: { baseURL: 'http://localhost:5174', viewport: { width: 1280, height: 800 } },
  webServer: [
    {
      command: 'dotnet run --project ../backend/src/FluxusManager.API --no-launch-profile',
      url: 'http://localhost:5259/health',
      timeout: 120_000,
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development', ASPNETCORE_URLS: 'http://localhost:5259',
        ConnectionStrings__DefaultConnection: database,
        DevelopmentSeed__Enabled: 'true',
        DevelopmentSeed__AdminPassword: 'senha-exclusiva-e2e-59',
      },
    },
    {
      command: 'npm run dev -- --host localhost --port 5174 --strictPort',
      url: 'http://localhost:5174',
      env: { API_PROXY_TARGET: 'http://localhost:5259' },
    },
  ],
})
