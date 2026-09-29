import { http } from '@/core/api/http'
import type { PagedResult } from '@/shared/types/paged'
import type { Aprovacao, FiltrosSolicitacoes, Recusa, SolicitacaoDetalhe, SolicitacaoResumo } from '../types/solicitacao'

export const PAGE_SIZE = 20
const base = '/plataforma/solicitacoes-cadastro'

export async function listarSolicitacoes(page: number, filtros: FiltrosSolicitacoes) {
  // Filtro vazio não vai na URL.
  const params = { page, pageSize: PAGE_SIZE, status: filtros.status ?? undefined, de: filtros.de ?? undefined, ate: filtros.ate ?? undefined }
  const { data } = await http.get<PagedResult<SolicitacaoResumo>>(base, { params })
  return data
}

export async function obterSolicitacao(id: string) {
  const { data } = await http.get<SolicitacaoDetalhe>(`${base}/${id}`)
  return data
}

export async function aprovarSolicitacao(id: string, aprovacao: Aprovacao) {
  const { data } = await http.post<SolicitacaoDetalhe>(`${base}/${id}/aprovar`, aprovacao)
  return data
}

export async function recusarSolicitacao(id: string, recusa: Recusa) {
  const { data } = await http.post<SolicitacaoDetalhe>(`${base}/${id}/recusar`, recusa)
  return data
}

export async function reenviarEmails(id: string) {
  const { data } = await http.post<SolicitacaoDetalhe>(`${base}/${id}/reenviar-emails`)
  return data
}
