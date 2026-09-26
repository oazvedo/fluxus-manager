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
