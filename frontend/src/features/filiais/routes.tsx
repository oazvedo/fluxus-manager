import type { RouteObject } from 'react-router'

export const filiaisRoutes: RouteObject[] = [{ path: 'filiais', lazy: async () => ({ Component: (await import('./pages/FiliaisPage')).FiliaisPage }), handle: { title: 'Filiais' } }]
