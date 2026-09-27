import { http } from '@/core/api/http'
import type { PagedResult } from '@/shared/types/paged'
import type { AtualizarEmpresa, CriarEmpresa, Empresa } from '../types/empresa'

export const PAGE_SIZE = 20

export async function listarEmpresas(page: number) {
  const { data } = await http.get<PagedResult<Empresa>>('/empresas', { params: { page, pageSize: PAGE_SIZE } })
  return data
}

export async function obterEmpresa(id: string) {
  const { data } = await http.get<Empresa>(`/empresas/${id}`)
  return data
}

export async function criarEmpresa(empresa: CriarEmpresa) {
  const { data } = await http.post<Empresa>('/empresas', empresa)
  return data
}

export async function atualizarEmpresa(id: string, empresa: AtualizarEmpresa) {
  const { data } = await http.put<Empresa>(`/empresas/${id}`, empresa)
  return data
}

export async function alterarStatusEmpresa(id: string, ativo: boolean) {
  await http.patch(`/empresas/${id}/${ativo ? 'ativar' : 'inativar'}`)
}
