import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { AxiosHeaders, type InternalAxiosRequestConfig } from 'axios'
import { http } from '../src/core/api/http'
import { setSession } from '../src/core/auth/session'
import { aceitarConvite, consultarConvite, listarPerfisAtivos } from '../src/features/convites/api/convites'
import { tokenDoLink } from '../src/features/convites/lib/token'
import { aceiteNovoUsuarioSchema, conviteFormSchema } from '../src/features/convites/schemas/convite'

const token = 'A1'.repeat(32)
const adapter = http.defaults.adapter

function ok(config: InternalAxiosRequestConfig, data: unknown = {}) {
  return { data, status: 200, statusText: 'OK', config, headers: new AxiosHeaders() }
}

beforeEach(() => vi.stubGlobal('window', { location: { pathname: '/convites/aceitar' } }))
afterEach(() => {
  http.defaults.adapter = adapter
  setSession(null)
  vi.unstubAllGlobals()
})

describe('link do convite', () => {
  it('aceita só o token completo, em hexadecimal maiúsculo', () => {
    expect(tokenDoLink(`?token=${token}`)).toBe(token)
    expect(tokenDoLink(`?token=${token.slice(1)}`)).toBeNull()
    expect(tokenDoLink(`?token=${token.toLowerCase()}`)).toBeNull()
    expect(tokenDoLink('')).toBeNull()
  })

  it('envia o token no corpo, nunca na URL da API', async () => {
    const chamadas: InternalAxiosRequestConfig[] = []
    http.defaults.adapter = async (config) => {
      chamadas.push(config)
      return ok(config, { email: 'ana@acme.com' })
    }

    await consultarConvite(token)
    await aceitarConvite({ token, nome: 'Ana', senha: 'segredo123' })

    expect(chamadas.map((c) => [c.method, c.url])).toEqual([['post', '/convites/consultar'], ['post', '/convites/aceitar']])
    expect(chamadas.every((c) => !c.url?.includes(token) && JSON.parse(c.data as string).token === token)).toBe(true)
  })
})

describe('formulários de convite', () => {
  it('exige e-mail válido e perfil', () => {
    expect(conviteFormSchema.safeParse({ email: ' ana@acme.com ', perfilId: 'p1' }).data?.email).toBe('ana@acme.com')
    const erro = conviteFormSchema.safeParse({ email: 'ana', perfilId: '' })
    expect(erro.error?.issues.map((issue) => issue.path[0])).toEqual(['email', 'perfilId'])
  })

  it('aplica a regra de senha da API e confere a repetição', () => {
    expect(aceiteNovoUsuarioSchema.safeParse({ nome: 'Ana', senha: 'segredo123', confirmacao: 'segredo123' }).success).toBe(true)
    const fraca = aceiteNovoUsuarioSchema.safeParse({ nome: 'Ana', senha: 'somenteletras', confirmacao: 'somenteletras' })
    expect(fraca.error?.issues[0]?.message).toBe('A senha deve conter pelo menos um número.')
    const diferente = aceiteNovoUsuarioSchema.safeParse({ nome: 'Ana', senha: 'segredo123', confirmacao: 'segredo124' })
    expect(diferente.error?.issues.map((issue) => issue.path[0])).toEqual(['confirmacao'])
  })

  it('oferece só os perfis ativos', async () => {
    http.defaults.adapter = async (config) => ok(config, {
      items: [{ id: '1', nome: 'Administrador', ativo: true }, { id: '2', nome: 'Antigo', ativo: false }],
      page: 1, pageSize: 100, totalCount: 2, totalPages: 1,
    })

    expect((await listarPerfisAtivos()).map((perfil) => perfil.nome)).toEqual(['Administrador'])
  })
})
