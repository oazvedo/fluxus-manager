import type { RouteObject } from 'react-router'

export const dashboardRoutes: RouteObject[] = [
  {
    index: true,
    lazy: async () => ({ Component: (await import('./pages/HomePage')).HomePage }),
    handle: { title: 'Início' },
  },
]
