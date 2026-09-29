/** Situação da solicitação na API. "Expirado" não é estado: é o link (token) vencido, respondido com 410. */
export type SolicitacaoStatus = 'AguardandoVerificacao' | 'PendenteAnalise' | 'Aprovada' | 'Recusada'

export type NovaSolicitacao = {
  razaoSocial: string
  nomeFantasia: string | null
  cnpj: string
  responsavelNome: string
  responsavelEmail: string
  responsavelTelefone: string | null
}

/** O que o link de acompanhamento mostra: só o necessário, sem CNPJ nem dados internos. */
export type Acompanhamento = {
  status: SolicitacaoStatus
  razaoSocial: string
  criadaEm: string
  verificadaEm: string | null
  decididaEm: string | null
  motivoRecusa: string | null
}
