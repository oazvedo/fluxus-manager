import type { RouteObject } from 'react-router'

export const empresasRoutes: RouteObject[] = [
  {
    path: 'empresas',
    lazy: async () => ({ Component: (await import('./pages/EmpresasPage')).EmpresasPage }),
    handle: { title: 'Empresas' },
  },
]
