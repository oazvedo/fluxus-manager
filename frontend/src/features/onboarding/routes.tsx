import type { RouteObject } from 'react-router'
import { PageLoading } from '@/shared/components/common/PageLoading'

/** Fluxo público de quem ainda não tem conta: fora do layout autenticado e sem exigir sessão. */
export const solicitacaoCadastroRoutes: RouteObject[] = [{
  path: '/solicitar-cadastro',
  HydrateFallback: PageLoading,
  lazy: async () => ({ Component: (await import('./pages/SolicitarCadastroPage')).SolicitarCadastroPage }),
}, {
  path: '/solicitar-cadastro/verificar',
  HydrateFallback: PageLoading,
  lazy: async () => ({ Component: (await import('./pages/VerificarEmailPage')).VerificarEmailPage }),
}, {
  path: '/solicitar-cadastro/acompanhar',
  HydrateFallback: PageLoading,
  lazy: async () => ({ Component: (await import('./pages/AcompanharSolicitacaoPage')).AcompanharSolicitacaoPage }),
}]
