import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { AxiosError, AxiosHeaders, type InternalAxiosRequestConfig } from 'axios'
import { http } from '../src/core/api/http'
import { listarCadastrosRecentes, listarConvitesAtencao } from '../src/features/dashboard/api/inicio'
import { formatRelative } from '../src/features/dashboard/lib/relative-time'

const adapter = http.defaults.adapter
beforeEach(() => vi.stubGlobal('window', { location: { pathname: '/' } }))
afterEach(() => {
  http.defaults.adapter = adapter
  vi.unstubAllGlobals()
})

function pagina(items: unknown[], page: number, totalPages: number) {
  return { items, page, pageSize: 100, totalCount: items.length, totalPages, hasNextPage: page < totalPages, hasPreviousPage: page > 1 }
}

/** Responde cada rota com uma função de (path, page); rota ausente vira 403. */
function api(rotas: Record<string, (page: number) => unknown>) {
  const pedidos: string[] = []
  http.defaults.adapter = async (config: InternalAxiosRequestConfig) => {
    const page = Number(config.params?.page)
    pedidos.push(`${config.url}?page=${page}`)
    const rota = rotas[config.url ?? '']
    if (!rota) {
      const response = { data: {}, status: 403, statusText: 'Forbidden', config, headers: new AxiosHeaders() }
      throw new AxiosError('Forbidden', 'ERR_BAD_REQUEST', config, null, response)
    }
    return { data: rota(page), status: 200, statusText: 'OK', config, headers: new AxiosHeaders() }
  }
  return pedidos
}

describe('convites para acompanhar', () => {
  const agora = new Date('2026-09-28T12:00:00Z')

  it('traz vencidos e os que vencem em até 3 dias, o mais urgente primeiro', async () => {
    api({
      '/convites': () =>
        pagina(
          [
            { id: '1', email: 'a@x.com', perfil: 'Admin', status: 'Pendente', expiraEm: '2026-10-10T00:00:00Z' },
            { id: '2', email: 'b@x.com', perfil: 'Admin', status: 'Pendente', expiraEm: '2026-09-30T00:00:00Z' },
            { id: '3', email: 'c@x.com', perfil: 'Admin', status: 'Expirado', expiraEm: '2026-09-20T00:00:00Z' },
            { id: '4', email: 'd@x.com', perfil: 'Admin', status: 'Aceito', expiraEm: '2026-09-29T00:00:00Z' },
          ],
          1,
          1,
        ),
    })

    const convites = await listarConvitesAtencao(agora)
    expect(convites.map((c) => c.id)).toEqual(['3', '2'])
  })

  it('busca a última página, onde estão os convites mais novos', async () => {
    const pedidos = api({ '/convites': (page) => pagina(page === 3 ? Array(20).fill(null).map((_, i) => ({ id: `n${i}`, status: 'Aceito', expiraEm: '' })) : [], page, 3) })
    await listarConvitesAtencao(agora)
    expect(pedidos).toEqual(['/convites?page=1', '/convites?page=3'])
  })
})

describe('cadastrados por último', () => {
  it('junta as listas, ordena do mais novo e ignora a que falhar', async () => {
    api({
      '/empresas': () => pagina([{ id: 'e1', razaoSocial: 'ACME LTDA', nomeFantasia: null, ativo: true, criadoEm: '2026-09-01T00:00:00Z' }], 1, 1),
      '/filiais': () => pagina([{ id: 'f1', nome: 'Centro', ativo: false, criadoEm: '2026-09-10T00:00:00Z' }], 1, 1),
    })

    const itens = await listarCadastrosRecentes()
    expect(itens.map((i) => [i.tipo, i.nome])).toEqual([
      ['filial', 'Centro'],
      ['empresa', 'ACME LTDA'],
    ])
  })

  it('falha só quando nenhuma lista responde', async () => {
    api({})
    await expect(listarCadastrosRecentes()).rejects.toBeInstanceOf(AxiosError)
  })
})

describe('tempo relativo', () => {
  const agora = new Date('2026-09-28T12:00:00Z')

  it('escolhe a maior unidade que cabe', () => {
    expect(formatRelative('2026-09-28T11:59:30Z', agora)).toBe('agora')
    expect(formatRelative('2026-09-28T09:00:00Z', agora)).toBe('há 3 horas')
    expect(formatRelative('2026-09-27T12:00:00Z', agora)).toBe('ontem')
    expect(formatRelative('2026-09-30T12:00:00Z', agora)).toBe('depois de amanhã')
  })
})
