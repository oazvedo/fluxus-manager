import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { AxiosError, AxiosHeaders, type InternalAxiosRequestConfig } from 'axios'
import { http } from '../src/core/api/http'
import { acompanharSolicitacao, enviarSolicitacao, verificarEmail } from '../src/features/onboarding/api/solicitacao'
import { etapasDaSolicitacao } from '../src/features/onboarding/lib/etapas'
import { falhaDoLink, tokenDoLink } from '../src/features/onboarding/lib/link'
import { paraSolicitacao, solicitacaoSchema } from '../src/features/onboarding/schemas/solicitacao'
import type { Acompanhamento } from '../src/features/onboarding/types/solicitacao'
import { formatTelefone, isValidTelefone, normalizeTelefone } from '../src/shared/lib/telefone'

const token = 'B2'.repeat(32)
const adapter = http.defaults.adapter

beforeEach(() => vi.stubGlobal('window', { location: { pathname: '/solicitar-cadastro' } }))
afterEach(() => {
  http.defaults.adapter = adapter
  vi.unstubAllGlobals()
})

function falha(status: number) {
  const config = { headers: new AxiosHeaders() } as InternalAxiosRequestConfig
  return new AxiosError('x', 'ERR_BAD_REQUEST', config, null, { data: {}, status, statusText: '', config, headers: new AxiosHeaders() })
}

const valido = {
  razaoSocial: 'Acme Comércio Ltda', nomeFantasia: '', cnpj: '11.222.333/0001-81',
  responsavelNome: 'Ana Souza', responsavelEmail: ' ana@acme.com.br ', responsavelTelefone: '',
}

describe('formulário de solicitação', () => {
  it('aceita os dados mínimos e envia opcionais vazios como null, CNPJ só com os caracteres', () => {
    const values = solicitacaoSchema.parse(valido)
    expect(paraSolicitacao(values)).toEqual({
      razaoSocial: 'Acme Comércio Ltda', nomeFantasia: null, cnpj: '11222333000181',
      responsavelNome: 'Ana Souza', responsavelEmail: 'ana@acme.com.br', responsavelTelefone: null,
    })
  })

  it('aponta cada campo inválido', () => {
    const erro = solicitacaoSchema.safeParse({ ...valido, razaoSocial: ' ', cnpj: '11.222.333/0001-80', responsavelEmail: 'ana', responsavelTelefone: '123' })
    expect(erro.error?.issues.map((issue) => issue.path[0])).toEqual(['razaoSocial', 'cnpj', 'responsavelEmail', 'responsavelTelefone'])
  })

  it('telefone opcional: com DDD, 10 ou 11 dígitos, formatado enquanto se digita', () => {
    expect(normalizeTelefone('(11) 98765-4321')).toBe('11987654321')
    expect(formatTelefone('1198765')).toBe('(11) 9876-5')
    expect(formatTelefone('11987654321')).toBe('(11) 98765-4321')
    expect(formatTelefone('1134567890')).toBe('(11) 3456-7890')
    expect(isValidTelefone('1134567890')).toBe(true)
    expect(isValidTelefone('0134567890')).toBe(false)
    const values = solicitacaoSchema.parse({ ...valido, responsavelTelefone: '11987654321' })
    expect(paraSolicitacao(values).responsavelTelefone).toBe('11987654321')
  })
})

describe('links do e-mail', () => {
  it('aceita só o token completo, em hexadecimal maiúsculo', () => {
    expect(tokenDoLink(`?token=${token}`)).toBe(token)
    expect(tokenDoLink(`?token=${token.slice(2)}`)).toBeNull()
    expect(tokenDoLink(`?token=${token.toLowerCase()}`)).toBeNull()
  })

  it('distingue link inválido (404), vencido (410) e falha de rede', () => {
    expect(falhaDoLink(falha(404))).toBe('invalido')
    expect(falhaDoLink(falha(410))).toBe('expirado')
    expect(falhaDoLink(falha(500))).toBe('erro')
    expect(falhaDoLink(new Error('rede'))).toBe('erro')
  })

  it('envia o token no corpo, nunca na URL da API', async () => {
    const chamadas: InternalAxiosRequestConfig[] = []
    http.defaults.adapter = async (config) => {
      chamadas.push(config)
      return { data: { status: 'PendenteAnalise' }, status: 200, statusText: 'OK', config, headers: new AxiosHeaders() }
    }
    await enviarSolicitacao(paraSolicitacao(solicitacaoSchema.parse(valido)))
    await verificarEmail(token)
    await acompanharSolicitacao(token)

    expect(chamadas.map((c) => [c.method, c.url])).toEqual([
      ['post', '/solicitacoes-cadastro'],
      ['post', '/solicitacoes-cadastro/verificar'],
      ['post', '/solicitacoes-cadastro/acompanhar'],
    ])
    expect(chamadas.slice(1).every((c) => !c.url?.includes(token) && JSON.parse(c.data as string).token === token)).toBe(true)
    expect(chamadas.every((c) => c.headers.get('Authorization') === undefined)).toBe(true)
  })
})

describe('linha do tempo do acompanhamento', () => {
  const base: Acompanhamento = {
    status: 'AguardandoVerificacao', razaoSocial: 'Acme', criadaEm: '2026-09-28T10:00:00Z',
    verificadaEm: null, decididaEm: null, motivoRecusa: null,
  }
  const situacoes = (a: Acompanhamento) => etapasDaSolicitacao(a).map((etapa) => etapa.situacao)

  it('marca uma etapa atual por vez até a decisão', () => {
    expect(situacoes(base)).toEqual(['feito', 'atual', 'pendente', 'pendente'])
    const verificada = { ...base, status: 'PendenteAnalise' as const, verificadaEm: '2026-09-28T11:00:00Z' }
    expect(situacoes(verificada)).toEqual(['feito', 'feito', 'atual', 'pendente'])
  })

  it('termina em aprovada ou recusada, sem etapa atual', () => {
    const decidida = { ...base, verificadaEm: '2026-09-28T11:00:00Z', decididaEm: '2026-09-29T09:00:00Z' }
    expect(situacoes({ ...decidida, status: 'Aprovada' })).toEqual(['feito', 'feito', 'feito', 'feito'])
    expect(situacoes({ ...decidida, status: 'Recusada' })).toEqual(['feito', 'feito', 'feito', 'recusado'])
  })
})
