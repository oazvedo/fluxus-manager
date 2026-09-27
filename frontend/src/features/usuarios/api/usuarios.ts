import { http } from '@/core/api/http'
import type { PagedResult } from '@/shared/types/paged'
import type { AtualizarUsuario, CriarUsuario, Usuario } from '../types/usuario'

export const PAGE_SIZE = 20

export async function listarUsuarios(page: number) {
  const { data } = await http.get<PagedResult<Usuario>>('/usuarios', { params: { page, pageSize: PAGE_SIZE } })
  return data
}

export async function obterUsuario(id: string) {
  const { data } = await http.get<Usuario>(`/usuarios/${id}`)
  return data
}

export async function criarUsuario(usuario: CriarUsuario) {
  const { data } = await http.post<Usuario>('/usuarios', usuario)
  return data
}

export async function atualizarUsuario(id: string, usuario: AtualizarUsuario) {
  const { data } = await http.put<Usuario>(`/usuarios/${id}`, usuario)
  return data
}

export async function alterarStatusUsuario(id: string, ativo: boolean) {
  await http.patch(`/usuarios/${id}/${ativo ? 'ativar' : 'inativar'}`)
}
