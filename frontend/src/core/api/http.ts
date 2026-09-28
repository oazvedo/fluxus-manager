import axios from 'axios'
import { env } from '@/core/config/env'
import { getSession, setSession, type Tokens } from '@/core/auth/session'

declare module 'axios' {
  interface InternalAxiosRequestConfig {
    sessionId?: string
    authRetried?: boolean
  }
}

/** Cliente único, com auditoria e renovação serializada da sessão nesta aba. */
export const http = axios.create({
  baseURL: env.apiUrl,
  timeout: 15_000,
})

// Tela que originou a requisição: a API grava na auditoria (audit.change_log.frontend_url).
http.interceptors.request.use((config) => {
  config.headers.set('X-Frontend-Url', window.location.pathname)
  const session = getSession()
  if (!isCredentialEndpoint(config.url) && session) {
    config.headers.set('Authorization', `Bearer ${session.accessToken}`)
    config.sessionId = session.id
  }
  return config
})

const isCredentialEndpoint = (url?: string) => /^\/auth\/(login|refresh|logout|forgot-password|reset-password)$/.test(url ?? '')
let refreshing: { sessionId: string; promise: Promise<void> } | null = null

async function refreshSession() {
  const original = getSession()
  if (!original) throw new Error('Sessão encerrada.')
  if (refreshing?.sessionId === original.id) return refreshing.promise

  const promise = (async () => {
    try {
      const { data } = await http.post<Tokens>('/auth/refresh', {
        refreshToken: original.refreshToken,
        empresaId: original.tenantId,
      })
      if (getSession()?.id === original.id) setSession({ ...original, ...data })
    } catch (error) {
      // Uma resposta perdida pode ter consumido o refresh token: nunca tentar reutilizá-lo.
      // A exceção é o 429: a API recusou antes de processar, e o token continua válido.
      const limitado = axios.isAxiosError(error) && error.response?.status === 429
      if (!limitado && getSession()?.id === original.id) setSession(null)
      throw error
    } finally {
      if (refreshing?.sessionId === original.id) refreshing = null
    }
  })()
  refreshing = { sessionId: original.id, promise }
  return promise
}

http.interceptors.response.use(undefined, async (error: unknown) => {
  if (!axios.isAxiosError(error)) throw error
  const config = error.config
  const session = getSession()
  if (error.response?.status !== 401 || !config || isCredentialEndpoint(config.url)
    || !session || config.sessionId !== session.id) throw error

  if (config.authRetried) {
    if (config.headers.get('Authorization') === `Bearer ${session.accessToken}`) setSession(null)
    throw error
  }

  config.authRetried = true
  // Um 401 tardio pode pertencer ao token anterior à renovação já concluída.
  if (config.headers.get('Authorization') === `Bearer ${session.accessToken}`) await refreshSession()
  if (getSession()?.id !== session.id) throw error
  return http.request(config)
})
