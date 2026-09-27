/** Usuário como a API devolve (UsuarioResponse). Nunca traz a senha; o e-mail vem em minúsculas. */
export type Usuario = {
  id: string
  nome: string
  email: string
  ativo: boolean
  criadoEm: string
  atualizadoEm: string | null
}

export type CriarUsuario = {
  nome: string
  email: string
  senha: string
}

/** A senha não é alterada por aqui: a troca de senha é um fluxo próprio. */
export type AtualizarUsuario = {
  nome: string
  email: string
}
