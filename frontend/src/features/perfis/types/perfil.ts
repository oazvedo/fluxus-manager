/** Perfil e catálogo de permissões como devolvidos pela API. */
export type Perfil = {
  id: string
  empresaId: string
  nome: string
  descricao: string | null
  ativo: boolean
  permissoes: string[]
  criadoEm: string
  atualizadoEm: string | null
}

export type Permissao = { codigo: string; nome: string; descricao: string }
export type SalvarPerfil = { nome: string; descricao: string; permissoes: string[] }
