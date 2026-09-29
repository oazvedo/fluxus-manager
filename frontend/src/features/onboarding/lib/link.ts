import { getProblem } from '@/core/api/problem'

/** Token do link do e-mail (`?token=`): 64 caracteres hexadecimais maiúsculos, ou null se faltar ou vier corrompido. */
export function tokenDoLink(search: string) {
  const token = new URLSearchParams(search).get('token')?.trim() ?? ''
  return /^[0-9A-F]{64}$/.test(token) ? token : null
}

export type FalhaDoLink = 'invalido' | 'expirado' | 'erro'

/** A API responde 404 para link inválido ou já usado e 410 para vencido; o resto é falha de rede ou do servidor. */
export function falhaDoLink(error: unknown): FalhaDoLink {
  const status = getProblem(error)?.status
  return status === 410 ? 'expirado' : status === 404 ? 'invalido' : 'erro'
}
