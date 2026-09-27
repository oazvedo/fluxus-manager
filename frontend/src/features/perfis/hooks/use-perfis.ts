import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  alterarStatusPerfil,
  atualizarPerfil,
  criarPerfil,
  excluirPerfil,
  listarPermissoes,
  listarPerfis,
  obterPerfil,
} from '../api/perfis'
import type { Perfil, SalvarPerfil } from '../types/perfil'

export const perfisKeys = {
  all: ['perfis'] as const,
  list: (page: number) => ['perfis', 'list', page] as const,
  detail: (id: string) => ['perfis', 'detail', id] as const,
  permissoes: ['perfis', 'permissoes'] as const,
}

export function usePerfis(page: number) {
  return useQuery({ queryKey: perfisKeys.list(page), queryFn: () => listarPerfis(page), placeholderData: keepPreviousData })
}

export function usePerfil(id: string | null) {
  const client = useQueryClient()
  return useQuery({
    queryKey: perfisKeys.detail(id ?? ''),
    queryFn: () => obterPerfil(id!),
    enabled: id !== null,
    retry: false,
    placeholderData: () => client.getQueriesData<{ items: Perfil[] }>({ queryKey: ['perfis', 'list'] })
      .flatMap(([, page]) => page?.items ?? []).find((perfil) => perfil.id === id),
  })
}

export function usePermissoes() {
  return useQuery({ queryKey: perfisKeys.permissoes, queryFn: listarPermissoes })
}

export function useCriarPerfil() {
  const client = useQueryClient()
  return useMutation({ mutationFn: criarPerfil, onSuccess: () => client.invalidateQueries({ queryKey: perfisKeys.all }) })
}

export function useAtualizarPerfil(id: string) {
  const client = useQueryClient()
  return useMutation({
    mutationFn: (perfil: SalvarPerfil) => atualizarPerfil(id, perfil),
    onSuccess: (perfil) => {
      client.setQueryData(perfisKeys.detail(id), perfil)
      return client.invalidateQueries({ queryKey: ['perfis', 'list'] })
    },
  })
}

export function useAlterarStatusPerfil() {
  const client = useQueryClient()
  return useMutation({ mutationFn: ({ id, ativo }: { id: string; ativo: boolean }) => alterarStatusPerfil(id, ativo),
    onSuccess: () => client.invalidateQueries({ queryKey: perfisKeys.all }) })
}

export function useExcluirPerfil() {
  const client = useQueryClient()
  return useMutation({ mutationFn: excluirPerfil, onSuccess: () => client.invalidateQueries({ queryKey: perfisKeys.all }) })
}
