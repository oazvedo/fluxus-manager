import { Outlet } from 'react-router'
import { useSession } from '@/core/auth/session'
import { SemPermissao } from './SemPermissao'

/** Esconde a área da plataforma de quem não é da equipe Fluxus. A API recusa as chamadas de qualquer forma (403). */
export function RequirePlatformAdmin() {
  const session = useSession()
  return session?.administradorPlataforma ? <Outlet /> : <SemPermissao />
}
