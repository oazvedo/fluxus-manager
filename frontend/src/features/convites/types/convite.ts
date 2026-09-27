/** Convite como devolvido pela API. `Expirado` é calculado pela API para convite pendente vencido. */
export type ConviteStatus = 'Pendente' | 'Expirado' | 'Aceito' | 'Cancelado'

export type Convite = {
  id: string
  email: string
  perfilId: string
  perfil: string
  status: ConviteStatus
  expiraEm: string
  aceitoEm: string | null
  canceladoEm: string | null
  criadoEm: string
  atualizadoEm: string | null
}

export type CriarConvite = { email: string; perfilId: string }

/** Dados da tela pública de aceite. */
export type ConviteDetalhes = {
  email: string
  empresa: string
  perfil: string
  expiraEm: string
  usuarioExistente: boolean
}

export type AceitarConvite = { token: string; nome: string | null; senha: string | null }

/** Perfil oferecido no convite (só os ativos da empresa). */
export type PerfilOpcao = { id: string; nome: string; ativo: boolean }
