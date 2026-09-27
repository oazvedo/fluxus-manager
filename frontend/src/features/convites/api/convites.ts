import { http } from '@/core/api/http'
import type { PagedResult } from '@/shared/types/paged'
import type { AceitarConvite, Convite, ConviteDetalhes, CriarConvite, PerfilOpcao } from '../types/convite'

export const PAGE_SIZE = 20

export async function listarConvites(page: number) {
  const { data } = await http.get<PagedResult<Convite>>('/convites', { params: { page, pageSize: PAGE_SIZE } })
  return data
}

export async function criarConvite(convite: CriarConvite) {
  const { data } = await http.post<Convite>('/convites', convite)
  return data
}

export async function reenviarConvite(id: string) {
  const { data } = await http.post<Convite>(`/convites/${id}/reenviar`)
  return data
}

export async function cancelarConvite(id: string) {
  await http.patch(`/convites/${id}/cancelar`)
}

/** Perfis que podem ser escolhidos no convite. A API aceita no máximo 100 por página. */
export async function listarPerfisAtivos() {
  const { data } = await http.get<PagedResult<PerfilOpcao>>('/perfis', { params: { page: 1, pageSize: 100 } })
  return data.items.filter((perfil) => perfil.ativo)
}

// Rotas públicas: o token vai no corpo, nunca na URL da API.
export async function consultarConvite(token: string) {
  const { data } = await http.post<ConviteDetalhes>('/convites/consultar', { token })
  return data
}

export async function aceitarConvite(aceite: AceitarConvite) {
  await http.post('/convites/aceitar', aceite)
}
