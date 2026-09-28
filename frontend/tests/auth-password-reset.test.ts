import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { AxiosHeaders, type InternalAxiosRequestConfig } from 'axios'
import { http } from '../src/core/api/http'
import { setSession } from '../src/core/auth/session'
import { redefinirSenha, solicitarRedefinicaoSenha } from '../src/features/auth/api/password-reset'
import { tokenDeRedefinicao } from '../src/features/auth/lib/reset-token'
import { esqueciSenhaSchema, redefinirSenhaSchema } from '../src/features/auth/schemas/password-reset'

const token = 'A1'.repeat(32)
const adapter = http.defaults.adapter

function ok(config: InternalAxiosRequestConfig, status = 200) {
  return { data: {}, status, statusText: 'OK', config, headers: new AxiosHeaders() }
}

beforeEach(() => vi.stubGlobal('window', { location: { pathname: '/redefinir-senha' } }))
afterEach(() => {
  http.defaults.adapter = adapter
  setSession(null)
  vi.unstubAllGlobals()
})

describe('recuperação de senha', () => {
  it('lê apenas token completo em hexadecimal maiúsculo', () => {
    expect(tokenDeRedefinicao(`?token=${token}`)).toBe(token)
    expect(tokenDeRedefinicao(`?token=${token.slice(1)}`)).toBeNull()
    expect(tokenDeRedefinicao(`?token=${token.toLowerCase()}`)).toBeNull()
    expect(tokenDeRedefinicao('')).toBeNull()
  })

  it('valida o e-mail e repete a política de senha da API', () => {
    expect(esqueciSenhaSchema.safeParse({ email: ' pessoa@exemplo.com ' }).data?.email).toBe('pessoa@exemplo.com')
    expect(esqueciSenhaSchema.safeParse({ email: 'invalido' }).success).toBe(false)
    expect(redefinirSenhaSchema.safeParse({ novaSenha: 'segredo123', confirmacao: 'segredo123' }).success).toBe(true)
    expect(redefinirSenhaSchema.safeParse({ novaSenha: 'somenteletras', confirmacao: 'somenteletras' }).success).toBe(false)
    expect(redefinirSenhaSchema.safeParse({ novaSenha: 'segredo123', confirmacao: 'segredo124' }).error?.issues[0]?.path)
      .toEqual(['confirmacao'])
  })

  it('manda o token no corpo, sem colocá-lo na URL ou enviar a sessão', async () => {
    const chamadas: InternalAxiosRequestConfig[] = []
    setSession({
      id: 'sessao', email: 'pessoa@exemplo.com', accessToken: 'acesso', tokenType: 'Bearer', expiresIn: 900,
      tenantId: 'empresa', role: 'Administrador', permissions: [], refreshToken: 'refresh',
      refreshTokenExpiresAt: '2099-01-01T00:00:00Z',
    })
    http.defaults.adapter = async (config) => {
      chamadas.push(config)
      return ok(config, config.url?.endsWith('reset-password') ? 204 : 200)
    }

    await solicitarRedefinicaoSenha('pessoa@exemplo.com')
    await redefinirSenha(token, 'segredo123')

    expect(chamadas.map(({ url }) => url)).toEqual(['/auth/forgot-password', '/auth/reset-password'])
    expect(chamadas.every(({ headers }) => !headers.get('Authorization'))).toBe(true)
    expect(chamadas[0].data).toBe(JSON.stringify({ email: 'pessoa@exemplo.com' }))
    expect(chamadas[1].data).toBe(JSON.stringify({ token, novaSenha: 'segredo123' }))
    expect(chamadas.every(({ url }) => !url?.includes(token))).toBe(true)
  })
})
