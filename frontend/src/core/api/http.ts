import axios from 'axios'
import { env } from '@/core/config/env'

/**
 * Cliente HTTP único da aplicação. Interceptors de autenticação (JWT) e de tenant
 * entram aqui quando o login existir; as features só usam este cliente.
 */
export const http = axios.create({
  baseURL: env.apiUrl,
  timeout: 15_000,
})

// Tela que originou a requisição: a API grava na auditoria (audit.change_log.frontend_url).
http.interceptors.request.use((config) => {
  config.headers.set('X-Frontend-Url', window.location.pathname)
  return config
})
