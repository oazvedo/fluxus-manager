import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { alterarStatusFilial, atualizarFilial, criarFilial, listarFiliais, obterFilial } from '../api/filiais'
import type { AtualizarFilial, Filial } from '../types/filial'

export const filiaisKeys = { all: ['filiais'] as const, list: (page: number) => ['filiais', 'list', page] as const, detail: (id: string) => ['filiais', 'detail', id] as const }
export function useFiliais(page: number) {
  return useQuery({ queryKey: filiaisKeys.list(page), queryFn: () => listarFiliais(page), placeholderData: keepPreviousData })
}
export function useFilial(id: string | null) {
  const client = useQueryClient()
  return useQuery({
    queryKey: filiaisKeys.detail(id ?? ''), queryFn: () => obterFilial(id!), enabled: id !== null, retry: false,
    placeholderData: () => client.getQueriesData<{ items: Filial[] }>({ queryKey: ['filiais', 'list'] }).flatMap(([, p]) => p?.items ?? []).find((f) => f.id === id),
  })
}
export function useCriarFilial() {
  const client = useQueryClient()
  return useMutation({ mutationFn: criarFilial, onSuccess: () => client.invalidateQueries({ queryKey: filiaisKeys.all }) })
}
export function useAtualizarFilial(id: string) {
  const client = useQueryClient()
  return useMutation({ mutationFn: (f: AtualizarFilial) => atualizarFilial(id, f), onSuccess: (f) => { client.setQueryData(filiaisKeys.detail(id), f); return client.invalidateQueries({ queryKey: ['filiais', 'list'] }) } })
}
export function useAlterarStatusFilial() {
  const client = useQueryClient()
  return useMutation({ mutationFn: ({ id, ativo }: { id: string; ativo: boolean }) => alterarStatusFilial(id, ativo), onSuccess: () => client.invalidateQueries({ queryKey: filiaisKeys.all }) })
}
