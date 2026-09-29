import { http } from '@/core/api/http'
import type { Acompanhamento, NovaSolicitacao, SolicitacaoStatus } from '../types/solicitacao'

// Rotas públicas. A resposta do envio é sempre neutra: não diz se o CNPJ ou o e-mail já existem.
// Tokens vão no corpo, nunca na URL da API.

export async function enviarSolicitacao(solicitacao: NovaSolicitacao) {
  await http.post('/solicitacoes-cadastro', solicitacao)
}

export async function verificarEmail(token: string) {
  const { data } = await http.post<{ status: SolicitacaoStatus }>('/solicitacoes-cadastro/verificar', { token })
  return data
}

export async function reenviarVerificacao(email: string) {
  await http.post('/solicitacoes-cadastro/reenviar-verificacao', { email })
}

export async function acompanharSolicitacao(token: string) {
  const { data } = await http.post<Acompanhamento>('/solicitacoes-cadastro/acompanhar', { token })
  return data
}
