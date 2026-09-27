import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  aceitarConvite,
  cancelarConvite,
  consultarConvite,
  criarConvite,
  listarConvites,
  listarPerfisAtivos,
  reenviarConvite,
} from '../api/convites'

export const convitesKeys = {
  all: ['convites'] as const,
  list: (page: number) => ['convites', 'list', page] as const,
  perfis: ['convites', 'perfis'] as const,
  detalhes: (token: string) => ['convites', 'detalhes', token] as const,
}

export function useConvites(page: number) {
  return useQuery({ queryKey: convitesKeys.list(page), queryFn: () => listarConvites(page), placeholderData: keepPreviousData })
}

export function usePerfisAtivos() {
  return useQuery({ queryKey: convitesKeys.perfis, queryFn: listarPerfisAtivos })
}

export function useCriarConvite() {
  const client = useQueryClient()
  return useMutation({ mutationFn: criarConvite, onSuccess: () => client.invalidateQueries({ queryKey: ['convites', 'list'] }) })
}

export function useReenviarConvite() {
  const client = useQueryClient()
  return useMutation({ mutationFn: reenviarConvite, onSuccess: () => client.invalidateQueries({ queryKey: ['convites', 'list'] }) })
}

export function useCancelarConvite() {
  const client = useQueryClient()
  return useMutation({ mutationFn: cancelarConvite, onSuccess: () => client.invalidateQueries({ queryKey: ['convites', 'list'] }) })
}

/** Tela pública: 404/422 descrevem o link (não encontrado, expirado...), então não se tenta de novo. */
export function useConviteDetalhes(token: string | null) {
  return useQuery({
    queryKey: convitesKeys.detalhes(token ?? ''),
    queryFn: () => consultarConvite(token!),
    enabled: token !== null,
    retry: false,
    staleTime: Infinity,
  })
}

export function useAceitarConvite() {
  return useMutation({ mutationFn: aceitarConvite, retry: false })
}
