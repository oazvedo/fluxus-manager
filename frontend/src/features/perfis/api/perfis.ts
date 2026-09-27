import { http } from '@/core/api/http'
import type { PagedResult } from '@/shared/types/paged'
import type { Perfil, Permissao, SalvarPerfil } from '../types/perfil'

export const PAGE_SIZE = 20

export async function listarPerfis(page: number) {
  const { data } = await http.get<PagedResult<Perfil>>('/perfis', { params: { page, pageSize: PAGE_SIZE } })
  return data
}

export async function obterPerfil(id: string) {
  const { data } = await http.get<Perfil>(`/perfis/${id}`)
  return data
}

export async function listarPermissoes() {
  const { data } = await http.get<Permissao[]>('/perfis/permissoes')
  return data
}

export async function criarPerfil(perfil: SalvarPerfil) {
  const { data } = await http.post<Perfil>('/perfis', perfil)
  return data
}

export async function atualizarPerfil(id: string, perfil: SalvarPerfil) {
  const { data } = await http.put<Perfil>(`/perfis/${id}`, perfil)
  return data
}

export async function alterarStatusPerfil(id: string, ativo: boolean) {
  await http.patch(`/perfis/${id}/${ativo ? 'ativar' : 'inativar'}`)
}

export async function excluirPerfil(id: string) {
  await http.delete(`/perfis/${id}`)
}
