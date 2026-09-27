import { http } from '@/core/api/http'
import type { PagedResult } from '@/shared/types/paged'
import type { AtualizarFilial, CriarFilial, Filial } from '../types/filial'

export const PAGE_SIZE = 20
export async function listarFiliais(page: number) {
  const { data } = await http.get<PagedResult<Filial>>('/filiais', { params: { page, pageSize: PAGE_SIZE } })
  return data
}
export async function obterFilial(id: string) {
  const { data } = await http.get<Filial>(`/filiais/${id}`)
  return data
}
export async function criarFilial(filial: CriarFilial) {
  const { data } = await http.post<Filial>('/filiais', filial)
  return data
}
export async function atualizarFilial(id: string, filial: AtualizarFilial) {
  const { data } = await http.put<Filial>(`/filiais/${id}`, filial)
  return data
}
export async function alterarStatusFilial(id: string, ativo: boolean) {
  await http.patch(`/filiais/${id}/${ativo ? 'ativar' : 'inativar'}`)
}
