import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { aprovarSolicitacao, listarSolicitacoes, obterSolicitacao, recusarSolicitacao, reenviarEmails } from '../api/solicitacoes'
import type { Aprovacao, FiltrosSolicitacoes, Recusa, SolicitacaoDetalhe } from '../types/solicitacao'

export const solicitacoesKeys = {
  list: (page: number, filtros: FiltrosSolicitacoes) => ['plataforma', 'solicitacoes', 'list', page, filtros] as const,
  detail: (id: string) => ['plataforma', 'solicitacoes', 'detail', id] as const,
}

export function useSolicitacoes(page: number, filtros: FiltrosSolicitacoes) {
  return useQuery({
    queryKey: solicitacoesKeys.list(page, filtros),
    queryFn: () => listarSolicitacoes(page, filtros),
    placeholderData: keepPreviousData,
  })
}

export function useSolicitacao(id: string) {
  return useQuery({ queryKey: solicitacoesKeys.detail(id), queryFn: () => obterSolicitacao(id) })
}

/** Toda decisão devolve o detalhe atualizado: vira o cache do painel, e a fila é recarregada. */
function useDecisao<T>(id: string, mutationFn: (values: T) => Promise<SolicitacaoDetalhe>) {
  const client = useQueryClient()
  return useMutation({
    mutationFn,
    retry: false,
    onSuccess: (detalhe) => {
      client.setQueryData(solicitacoesKeys.detail(id), detalhe)
      return client.invalidateQueries({ queryKey: ['plataforma', 'solicitacoes', 'list'] })
    },
  })
}

export const useAprovarSolicitacao = (id: string) => useDecisao(id, (values: Aprovacao) => aprovarSolicitacao(id, values))
export const useRecusarSolicitacao = (id: string) => useDecisao(id, (values: Recusa) => recusarSolicitacao(id, values))
export const useReenviarEmails = (id: string) => useDecisao(id, () => reenviarEmails(id))
