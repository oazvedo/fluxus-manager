import type { RouteObject } from 'react-router'
import { PageLoading } from '@/shared/components/common/PageLoading'

/** Tela administrativa, dentro do layout autenticado. */
export const convitesRoutes: RouteObject[] = [
  {
    path: 'convites',
    lazy: async () => ({ Component: (await import('./pages/ConvitesPage')).ConvitesPage }),
    handle: { title: 'Convites' },
  },
]

/** Link do e-mail: público, fora do layout e sem exigir sessão. */
export const aceiteConviteRoutes: RouteObject[] = [{
  path: '/convites/aceitar',
  HydrateFallback: PageLoading,
  lazy: async () => ({ Component: (await import('./pages/AceitarConvitePage')).AceitarConvitePage }),
}]
