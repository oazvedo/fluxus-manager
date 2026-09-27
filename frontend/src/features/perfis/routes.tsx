import type { RouteObject } from 'react-router'

export const perfisRoutes: RouteObject[] = [
  {
    path: 'perfis',
    lazy: async () => ({ Component: (await import('./pages/PerfisPage')).PerfisPage }),
    handle: { title: 'Perfis e permissões' },
  },
]
