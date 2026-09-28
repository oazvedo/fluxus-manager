import { http } from '@/core/api/http'
import type { PagedResult } from '@/shared/types/paged'

// A API aceita no máximo 100 itens por página e lista do mais antigo para o mais novo.
const MAX_PAGE_SIZE = 100

/** Os registros criados por último: a última página, mais a anterior se a última vier quase vazia. */
async function ultimosCriados<T>(path: string): Promise<T[]> {
  const get = (page: number) =>
    http.get<PagedResult<T>>(path, { params: { page, pageSize: MAX_PAGE_SIZE } }).then(({ data }) => data)

  const primeira = await get(1)
  const total = primeira.totalPages
  if (total <= 1) return primeira.items

  const ultima = await get(total)
  if (ultima.items.length >= 10) return ultima.items
  const anterior = total === 2 ? primeira : await get(total - 1)
  return [...anterior.items, ...ultima.items]
}

export type TipoCadastro = 'empresa' | 'filial' | 'usuario'

export type CadastroRecente = {
  id: string
  tipo: TipoCadastro
  nome: string
  ativo: boolean
  criadoEm: string
}

type Registro = { id: string; ativo: boolean; criadoEm: string }
type Empresa = Registro & { razaoSocial: string; nomeFantasia: string | null }
type ComNome = Registro & { nome: string }

async function recentes<T extends Registro>(path: string, tipo: TipoCadastro, nome: (item: T) => string) {
  const lista = await ultimosCriados<T>(path)
  return lista.map((item): CadastroRecente => ({ id: item.id, tipo, nome: nome(item), ativo: item.ativo, criadoEm: item.criadoEm }))
}

/**
 * Empresas, filiais e usuários criados por último, juntos e do mais novo para o mais antigo.
 * Uma lista que falha (ex.: sem permissão) só fica de fora; se todas falharem, o erro sobe.
 */
export async function listarCadastrosRecentes(limite = 8): Promise<CadastroRecente[]> {
  const resultados = await Promise.allSettled([
    recentes<Empresa>('/empresas', 'empresa', (e) => e.nomeFantasia || e.razaoSocial),
    recentes<ComNome>('/filiais', 'filial', (f) => f.nome),
    recentes<ComNome>('/usuarios', 'usuario', (u) => u.nome),
  ])

  const ok = resultados.flatMap((r) => (r.status === 'fulfilled' ? [r.value] : []))
  if (ok.length === 0) throw (resultados[0] as PromiseRejectedResult).reason

  return ok
    .flat()
    .sort((a, b) => b.criadoEm.localeCompare(a.criadoEm))
    .slice(0, limite)
}

export type ConviteAtencao = {
  id: string
  email: string
  perfil: string
  status: 'Pendente' | 'Expirado'
  expiraEm: string
}

/** Dias antes do vencimento a partir dos quais um convite pendente pede atenção. */
export const DIAS_AVISO_VENCIMENTO = 3

/** Convites vencidos ou que vencem nos próximos dias, o mais urgente primeiro. */
export async function listarConvitesAtencao(agora = new Date()): Promise<ConviteAtencao[]> {
  const limite = agora.getTime() + DIAS_AVISO_VENCIMENTO * 24 * 60 * 60 * 1000
  const convites = await ultimosCriados<Omit<ConviteAtencao, 'status'> & { status: string }>('/convites')

  return convites
    .filter((c): c is ConviteAtencao =>
      c.status === 'Expirado' || (c.status === 'Pendente' && new Date(c.expiraEm).getTime() <= limite))
    .sort((a, b) => a.expiraEm.localeCompare(b.expiraEm))
    .map(({ id, email, perfil, status, expiraEm }) => ({ id, email, perfil, status, expiraEm }))
}
