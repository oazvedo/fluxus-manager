/** Empresa como a API devolve (EmpresaResponse). O CNPJ vem normalizado, sem pontuação. */
export type Empresa = {
  id: string
  razaoSocial: string
  nomeFantasia: string | null
  cnpj: string
  ativo: boolean
  criadoEm: string
  atualizadoEm: string | null
}

export type CriarEmpresa = {
  razaoSocial: string
  nomeFantasia: string | null
  cnpj: string
}

/** O CNPJ não é alterável depois do cadastro. */
export type AtualizarEmpresa = {
  razaoSocial: string
  nomeFantasia: string | null
}
