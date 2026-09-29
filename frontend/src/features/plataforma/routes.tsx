import type { RouteObject } from 'react-router'
import { RequirePlatformAdmin } from './components/RequirePlatformAdmin'

/** Painel da equipe Fluxus, dentro do layout autenticado e separado do contexto de empresa. */
export const plataformaRoutes: RouteObject[] = [
  {
    path: 'plataforma',
    Component: RequirePlatformAdmin,
    children: [
      {
        path: 'solicitacoes',
        lazy: async () => ({ Component: (await import('./pages/SolicitacoesPage')).SolicitacoesPage }),
        handle: { title: 'Solicitações de cadastro' },
      },
    ],
  },
]
