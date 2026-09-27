import { http } from '@/core/api/http'
import { getSession, setSession, type Tokens } from './session'

export async function entrar(credentials: { email: string; senha: string }) {
  const { data } = await http.post<Tokens>('/auth/login', credentials)
  setSession({ ...data, email: credentials.email, id: crypto.randomUUID() })
}

export async function sair() {
  const previous = getSession()
  setSession(null)
  // O servidor aceita também um ancestral consumido durante renovação concorrente.
  if (previous) await http.post('/auth/logout', { refreshToken: previous.refreshToken })
}
