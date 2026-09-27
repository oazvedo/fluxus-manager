import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  alterarStatusEmpresa,
  atualizarEmpresa,
  criarEmpresa,
  listarEmpresas,
  obterEmpresa,
} from '../api/empresas'
import type { AtualizarEmpresa, Empresa } from '../types/empresa'

export const empresasKeys = {
  all: ['empresas'] as const,
  list: (page: number) => ['empresas', 'list', page] as const,
  detail: (id: string) => ['empresas', 'detail', id] as const,
}

export function useEmpresas(page: number) {
  return useQuery({
    queryKey: empresasKeys.list(page),
    queryFn: () => listarEmpresas(page),
    // Mantém a página anterior na tela enquanto a próxima carrega: sem piscar skeleton ao paginar.
    placeholderData: keepPreviousData,
  })
}

export function useEmpresa(id: string | null) {
  const queryClient = useQueryClient()

  return useQuery({
    queryKey: empresasKeys.detail(id ?? ''),
    queryFn: () => obterEmpresa(id!),
    enabled: id !== null,
    retry: false,
    // Abre o painel já preenchido com a linha da lista, se ela estiver em cache.
    placeholderData: () =>
      queryClient
        .getQueriesData<{ items: Empresa[] }>({ queryKey: ['empresas', 'list'] })
        .flatMap(([, page]) => page?.items ?? [])
        .find((empresa) => empresa.id === id),
  })
}

export function useCriarEmpresa() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: criarEmpresa,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: empresasKeys.all }),
  })
}

export function useAtualizarEmpresa(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (empresa: AtualizarEmpresa) => atualizarEmpresa(id, empresa),
    onSuccess: (empresa) => {
      queryClient.setQueryData(empresasKeys.detail(id), empresa)
      return queryClient.invalidateQueries({ queryKey: ['empresas', 'list'] })
    },
  })
}

export function useAlterarStatusEmpresa() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, ativo }: { id: string; ativo: boolean }) => alterarStatusEmpresa(id, ativo),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: empresasKeys.all }),
  })
}
