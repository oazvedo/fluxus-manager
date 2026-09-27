import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { AxiosError, AxiosHeaders, type InternalAxiosRequestConfig } from 'axios'
import { http } from '../src/core/api/http'
import { getSession, setSession, type Session } from '../src/core/auth/session'
import { sair } from '../src/core/auth/actions'

const initial: Session = {
  id: 'session-1', email: 'admin@fluxus.local', accessToken: 'old-access', tokenType: 'Bearer',
  refreshToken: 'old-refresh', refreshTokenExpiresAt: '2099-01-01T00:00:00Z',
  expiresIn: 900, tenantId: 'company-1', role: 'Administrador', permissions: [],
}
const rotated = { ...initial, accessToken: 'new-access', refreshToken: 'new-refresh' }
const adapter = http.defaults.adapter

function ok(config: InternalAxiosRequestConfig, data: unknown = {}) {
  return { data, status: 200, statusText: 'OK', config, headers: new AxiosHeaders() }
}
function reject(config: InternalAxiosRequestConfig, status = 401): never {
  throw new AxiosError('Falha', 'ERR_BAD_REQUEST', config, undefined, { ...ok(config), status })
}
function deferred() {
  let resolve!: () => void
  const promise = new Promise<void>((done) => { resolve = done })
  return { promise, resolve }
}

beforeEach(() => {
  vi.stubGlobal('window', { location: { pathname: '/empresas' } })
  setSession({ ...initial })
})
afterEach(() => {
  http.defaults.adapter = adapter
  setSession(null)
  vi.unstubAllGlobals()
})

describe('sessão HTTP', () => {
  it('envia Bearer e contexto da tela nas chamadas protegidas', async () => {
    http.defaults.adapter = async (config) => {
      expect(config.headers.get('Authorization')).toBe('Bearer old-access')
      expect(config.headers.get('X-Frontend-Url')).toBe('/empresas')
      return ok(config)
    }
    await http.get('/empresas')
  })

  it('serializa os 401 simultâneos e repete ambas as chamadas com o novo token', async () => {
    let refreshes = 0
    let requests = 0
    const gate = deferred()
    http.defaults.adapter = async (config) => {
      if (config.url === '/auth/refresh') {
        refreshes++
        expect(config.headers.has('Authorization')).toBe(false)
        await gate.promise
        return ok(config, rotated)
      }
      requests++
      if (config.headers.get('Authorization') === 'Bearer old-access') reject(config)
      return ok(config)
    }
    const responses = Promise.all([http.get('/empresas'), http.get('/usuarios')])
    await vi.waitFor(() => expect(refreshes).toBe(1))
    gate.resolve()
    await responses
    expect(refreshes).toBe(1)
    expect(requests).toBe(4)
    expect(getSession()?.refreshToken).toBe('new-refresh')
  })

  it('um 401 tardio reutiliza o access token já renovado', async () => {
    let refreshes = 0
    const gate = deferred()
    http.defaults.adapter = async (config) => {
      if (config.url === '/auth/refresh') { refreshes++; return ok(config, rotated) }
      if (config.headers.get('Authorization') === 'Bearer old-access') {
        if (config.url === '/usuarios') await gate.promise
        reject(config)
      }
      return ok(config)
    }
    const delayed = http.get('/usuarios')
    await http.get('/empresas')
    gate.resolve()
    await delayed
    expect(refreshes).toBe(1)
  })

  it.each([401, 500])('limpa a sessão quando a renovação falha (%s), sem loop', async (status) => {
    let calls = 0
    http.defaults.adapter = async (config) => { calls++; reject(config, config.url === '/auth/refresh' ? status : 401) }
    await expect(http.get('/empresas')).rejects.toBeDefined()
    expect(calls).toBe(2)
    expect(getSession()).toBeNull()
  })

  it('não renova 403 nem credenciais inválidas no login', async () => {
    let calls = 0
    http.defaults.adapter = async (config) => { calls++; reject(config, config.url === '/auth/login' ? 401 : 403) }
    await expect(http.get('/empresas')).rejects.toBeDefined()
    await expect(http.post('/auth/login', {})).rejects.toBeDefined()
    expect(calls).toBe(2)
    expect(getSession()?.id).toBe(initial.id)
  })

  it('encerra a sessão se a chamada continua sem autorização após renovar', async () => {
    let calls = 0
    http.defaults.adapter = async (config) => {
      calls++
      if (config.url === '/auth/refresh') return ok(config, rotated)
      reject(config)
    }
    await expect(http.get('/empresas')).rejects.toBeDefined()
    expect(calls).toBe(3)
    expect(getSession()).toBeNull()
  })

  it('logout durante a renovação não restaura a sessão', async () => {
    const gate = deferred()
    let refreshing = false
    http.defaults.adapter = async (config) => {
      if (config.url === '/auth/logout') {
        expect(JSON.parse(config.data).refreshToken).toBe('old-refresh')
        return ok(config)
      }
      if (config.url === '/auth/refresh') { refreshing = true; await gate.promise; return ok(config, rotated) }
      reject(config)
    }
    const request = http.get('/empresas').catch(() => undefined)
    await vi.waitFor(() => expect(refreshing).toBe(true))
    await sair()
    gate.resolve()
    await request
    expect(getSession()).toBeNull()
  })

  it('uma renovação antiga não sobrescreve um novo login', async () => {
    const gate = deferred()
    let refreshing = false
    http.defaults.adapter = async (config) => {
      if (config.url === '/auth/refresh') { refreshing = true; await gate.promise; return ok(config, rotated) }
      reject(config)
    }
    const request = http.get('/empresas').catch(() => undefined)
    await vi.waitFor(() => expect(refreshing).toBe(true))
    setSession({ ...initial, id: 'new-login', accessToken: 'independent' })
    gate.resolve()
    await request
    expect(getSession()?.accessToken).toBe('independent')
  })
})
