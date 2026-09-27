import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  alterarStatusUsuario,
  atualizarUsuario,
  criarUsuario,
  listarUsuarios,
  obterUsuario,
} from '../api/usuarios'
import type { AtualizarUsuario, Usuario } from '../types/usuario'

export const usuariosKeys = {
  all: ['usuarios'] as const,
  list: (page: number) => ['usuarios', 'list', page] as const,
  detail: (id: string) => ['usuarios', 'detail', id] as const,
}

export function useUsuarios(page: number) {
  return useQuery({
    queryKey: usuariosKeys.list(page),
    queryFn: () => listarUsuarios(page),
    // Mantém a página anterior na tela enquanto a próxima carrega: sem piscar skeleton ao paginar.
    placeholderData: keepPreviousData,
  })
}

export function useUsuario(id: string | null) {
  const queryClient = useQueryClient()

  return useQuery({
    queryKey: usuariosKeys.detail(id ?? ''),
    queryFn: () => obterUsuario(id!),
    enabled: id !== null,
    retry: false,
    // Abre o painel já preenchido com a linha da lista, se ela estiver em cache.
    placeholderData: () =>
      queryClient
        .getQueriesData<{ items: Usuario[] }>({ queryKey: ['usuarios', 'list'] })
        .flatMap(([, page]) => page?.items ?? [])
        .find((usuario) => usuario.id === id),
  })
}

export function useCriarUsuario() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: criarUsuario,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: usuariosKeys.all }),
  })
}

export function useAtualizarUsuario(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (usuario: AtualizarUsuario) => atualizarUsuario(id, usuario),
    onSuccess: (usuario) => {
      queryClient.setQueryData(usuariosKeys.detail(id), usuario)
      return queryClient.invalidateQueries({ queryKey: ['usuarios', 'list'] })
    },
  })
}

export function useAlterarStatusUsuario() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, ativo }: { id: string; ativo: boolean }) => alterarStatusUsuario(id, ativo),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: usuariosKeys.all }),
  })
}
