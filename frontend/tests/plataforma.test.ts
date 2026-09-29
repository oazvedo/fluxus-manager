import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { AxiosHeaders, type InternalAxiosRequestConfig } from 'axios'
import { visibleNavigation } from '../src/app/layout/navigation'
import { http } from '../src/core/api/http'
import { aprovarSolicitacao, listarSolicitacoes, recusarSolicitacao } from '../src/features/plataforma/api/solicitacoes'
import { eventoRotulo, isStatus, tipoEmailRotulo } from '../src/features/plataforma/lib/rotulos'
import { aprovacaoSchema, recusaSchema } from '../src/features/plataforma/schemas/decisao'

const adapter = http.defaults.adapter
beforeEach(() => vi.stubGlobal('window', { location: { pathname: '/plataforma/solicitacoes' } }))
afterEach(() => {
  http.defaults.adapter = adapter
  vi.unstubAllGlobals()
})

function registrar() {
  const chamadas: InternalAxiosRequestConfig[] = []
  http.defaults.adapter = async (config) => {
    chamadas.push(config)
    return { data: { items: [] }, status: 200, statusText: 'OK', config, headers: new AxiosHeaders() }
  }
  return chamadas
}

describe('navegação da plataforma', () => {
  it('só administradores da plataforma veem o grupo, mesmo com todas as permissões da empresa', () => {
    const rotulos = (admin: boolean) => visibleNavigation(admin).map((group) => group.label)
    expect(rotulos(false)).not.toContain('Plataforma')
    expect(rotulos(true)).toContain('Plataforma')
  })
})

describe('fila de solicitações', () => {
  it('manda só os filtros preenchidos', async () => {
    const chamadas = registrar()
    await listarSolicitacoes(2, { status: null, de: null, ate: null })
    await listarSolicitacoes(1, { status: 'PendenteAnalise', de: '2026-09-01', ate: '2026-09-30' })
    expect(chamadas[0].params).toEqual({ page: 2, pageSize: 20, status: undefined, de: undefined, ate: undefined })
    expect(chamadas[1].params).toEqual({ page: 1, pageSize: 20, status: 'PendenteAnalise', de: '2026-09-01', ate: '2026-09-30' })
    expect(chamadas.every((c) => c.url === '/plataforma/solicitacoes-cadastro')).toBe(true)
  })

  it('status da URL só vale se for um estado conhecido', () => {
    expect(isStatus('Aprovada')).toBe(true)
    expect(isStatus('Qualquer')).toBe(false)
    expect(isStatus(null)).toBe(false)
  })

  it('evento ou e-mail novo da API ganha nome legível em vez de sumir', () => {
    expect(eventoRotulo('Verificada')).toBe('E-mail confirmado')
    expect(eventoRotulo('ConviteReenviado')).toBe('Convite reenviado')
    expect(tipoEmailRotulo('Aprovacao')).toBe('Aprovação e convite')
    expect(tipoEmailRotulo('LembreteAnalise')).toBe('Lembrete analise')
  })
})

describe('decisões', () => {
  it('recusa exige motivo de 10 a 500 caracteres; a observação interna é opcional', () => {
    expect(recusaSchema.safeParse({ motivo: 'CNPJ baixado na Receita.', observacaoInterna: '' }).success).toBe(true)
    expect(recusaSchema.safeParse({ motivo: '   curto  ', observacaoInterna: '' }).error?.issues[0]?.path).toEqual(['motivo'])
    expect(recusaSchema.safeParse({ motivo: 'x'.repeat(501), observacaoInterna: '' }).success).toBe(false)
    expect(aprovacaoSchema.safeParse({ observacaoInterna: 'x'.repeat(1001) }).success).toBe(false)
  })

  it('aprovar e recusar vão para a solicitação certa com o corpo do contrato', async () => {
    const chamadas = registrar()
    await aprovarSolicitacao('s1', { observacaoInterna: null })
    await recusarSolicitacao('s2', { motivo: 'CNPJ baixado na Receita.', observacaoInterna: 'Conferido' })
    expect(chamadas.map((c) => [c.method, c.url, JSON.parse(c.data as string)])).toEqual([
      ['post', '/plataforma/solicitacoes-cadastro/s1/aprovar', { observacaoInterna: null }],
      ['post', '/plataforma/solicitacoes-cadastro/s2/recusar', { motivo: 'CNPJ baixado na Receita.', observacaoInterna: 'Conferido' }],
    ])
  })
})
