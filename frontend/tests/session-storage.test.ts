import { afterEach, beforeEach, expect, it, vi } from 'vitest'
import type { Session } from '../src/core/auth/session'

const key = 'fluxus.session.v1'
const original: Session = {
  id: 'session-1', email: 'admin@fluxus.local', accessToken: 'access-1', tokenType: 'Bearer',
  refreshToken: 'refresh-1', refreshTokenExpiresAt: '2099-01-01T00:00:00Z',
  expiresIn: 900, tenantId: 'company-1', role: 'Administrador', permissions: ['empresas.visualizar'],
}
let values: Map<string, string>

beforeEach(() => {
  vi.resetModules()
  values = new Map()
  vi.stubGlobal('sessionStorage', {
    getItem: (name: string) => values.get(name) ?? null,
    setItem: (name: string, value: string) => { values.set(name, value) },
    removeItem: (name: string) => { values.delete(name) },
  })
})
afterEach(() => vi.unstubAllGlobals())

it('restaura a sessão após recarregar o módulo, incluindo o refresh rotacionado', async () => {
  const first = await import('../src/core/auth/session')
  first.setSession(original)
  const rotated = { ...original, accessToken: 'access-2', refreshToken: 'refresh-2' }
  first.setSession(rotated)
  vi.resetModules()
  const reloaded = await import('../src/core/auth/session')
  expect(reloaded.getSession()).toEqual(rotated)
})

it('logout remove a sessão persistida e impede restauração', async () => {
  const first = await import('../src/core/auth/session')
  first.setSession(original)
  first.setSession(null)
  expect(values.has(key)).toBe(false)
  vi.resetModules()
  expect((await import('../src/core/auth/session')).getSession()).toBeNull()
})

it.each([
  'json inválido',
  'null',
  JSON.stringify({ id: 'incompleta' }),
  JSON.stringify({ ...original, permissions: [123] }),
  JSON.stringify({ ...original, refreshTokenExpiresAt: '2020-01-01T00:00:00Z' }),
  JSON.stringify({ ...original, refreshTokenExpiresAt: 'inválido' }),
])('descarta uma sessão inválida ou expirada (%#)', async (stored) => {
  values.set(key, stored)
  expect((await import('../src/core/auth/session')).getSession()).toBeNull()
  expect(values.has(key)).toBe(false)
})

it('mantém o login em memória se o navegador bloquear o armazenamento', async () => {
  const unavailable = () => { throw new Error('Storage indisponível') }
  vi.stubGlobal('sessionStorage', { getItem: unavailable, setItem: unavailable, removeItem: unavailable })
  const session = await import('../src/core/auth/session')
  expect(session.getSession()).toBeNull()
  session.setSession(original)
  expect(session.getSession()).toEqual(original)
  session.setSession(null)
  expect(session.getSession()).toBeNull()
})
