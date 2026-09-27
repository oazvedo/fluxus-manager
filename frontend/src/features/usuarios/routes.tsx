import type { RouteObject } from 'react-router'

export const usuariosRoutes: RouteObject[] = [
  {
    path: 'usuarios',
    lazy: async () => ({ Component: (await import('./pages/UsuariosPage')).UsuariosPage }),
    handle: { title: 'Usuários' },
  },
]
