import { useSyncExternalStore } from 'react'

export type Tokens = {
  accessToken: string
  tokenType: string
  expiresIn: number
  tenantId: string
  role: string
  permissions: string[]
  refreshToken: string
  refreshTokenExpiresAt: string
  /** Equipe Fluxus: vê o painel da plataforma. Só controla a interface; a API autoriza cada chamada. */
  administradorPlataforma?: boolean
}

export type Session = Tokens & { id: string; email: string }

const storageKey = 'fluxus.session.v1'

function restoreSession(): Session | null {
  try {
    const stored: unknown = JSON.parse(sessionStorage.getItem(storageKey) ?? 'null')
    if (stored && typeof stored === 'object') {
      const value = stored as Record<string, unknown>
      const strings = ['id', 'email', 'accessToken', 'tokenType', 'tenantId', 'role', 'refreshToken', 'refreshTokenExpiresAt']
      if (strings.every((key) => typeof value[key] === 'string' && value[key].length > 0)
        && typeof value.expiresIn === 'number' && Number.isFinite(value.expiresIn) && value.expiresIn > 0
        && Array.isArray(value.permissions) && value.permissions.every((permission) => typeof permission === 'string')
        && Date.parse(value.refreshTokenExpiresAt as string) > Date.now()) return stored as Session
    }
  } catch {
    // JSON inválido ou armazenamento bloqueado não deve impedir o acesso à tela de login.
  }
  persistSession(null)
  return null
}

function persistSession(value: Session | null) {
  try {
    if (value) sessionStorage.setItem(storageKey, JSON.stringify(value))
    else sessionStorage.removeItem(storageKey)
  } catch {
    // Sem acesso ao armazenamento, a sessão continua disponível apenas em memória.
  }
}

// Restaura antes de montar as rotas para preservar também o caminho e os filtros após F5.
let session: Session | null = restoreSession()
const listeners = new Set<() => void>()

export const getSession = () => session

export function setSession(value: Session | null) {
  session = value
  persistSession(value)
  listeners.forEach((listener) => listener())
}

export function subscribeSession(listener: () => void) {
  listeners.add(listener)
  return () => { listeners.delete(listener) }
}

export function useSession() {
  return useSyncExternalStore(subscribeSession, getSession, () => null)
}
