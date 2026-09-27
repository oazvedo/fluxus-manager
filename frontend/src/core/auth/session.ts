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
}

export type Session = Tokens & { id: string; email: string }

// Credenciais ficam somente na memória desta aba, sem persistência no navegador.
let session: Session | null = null
const listeners = new Set<() => void>()

export const getSession = () => session

export function setSession(value: Session | null) {
  session = value
  listeners.forEach((listener) => listener())
}

export function subscribeSession(listener: () => void) {
  listeners.add(listener)
  return () => { listeners.delete(listener) }
}

export function useSession() {
  return useSyncExternalStore(subscribeSession, getSession, () => null)
}
