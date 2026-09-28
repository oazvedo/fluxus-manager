import { useQuery } from '@tanstack/react-query'
import { listarCadastrosRecentes, listarConvitesAtencao } from '../api/inicio'

export function useCadastrosRecentes() {
  return useQuery({ queryKey: ['dashboard', 'cadastros-recentes'], queryFn: () => listarCadastrosRecentes() })
}

export function useConvitesAtencao() {
  return useQuery({ queryKey: ['dashboard', 'convites-atencao'], queryFn: () => listarConvitesAtencao() })
}
