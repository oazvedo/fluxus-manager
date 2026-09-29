/** Mesmos estados da API (ver features/onboarding). */
export type SolicitacaoStatus = 'AguardandoVerificacao' | 'PendenteAnalise' | 'Aprovada' | 'Recusada'

export type EntregaStatus = 'Pendente' | 'Enviado' | 'Falhou'

/** Linha da fila: dados mínimos para decidir quem abrir. */
export type SolicitacaoResumo = {
  id: string
  razaoSocial: string
  nomeFantasia: string | null
  cnpj: string
  responsavelNome: string
  responsavelEmail: string
  status: SolicitacaoStatus
  criadaEm: string
  verificadaEm: string | null
  decididaEm: string | null
  /** Algum e-mail da solicitação ainda não saiu (pendente ou com falha). */
  emailPendente: boolean
}

export type SolicitacaoDetalhe = SolicitacaoResumo & {
  responsavelTelefone: string | null
  decididaPor: { id: string; nome: string } | null
  observacaoInterna: string | null
  motivoRecusa: string | null
  empresaId: string | null
  historico: { evento: string; em: string; por: { id: string; nome: string } | null }[]
  emails: { tipo: string; status: EntregaStatus; tentativas: number; ultimaTentativaEm: string | null }[]
}

export type FiltrosSolicitacoes = {
  status: SolicitacaoStatus | null
  /** Datas de recebimento, "aaaa-mm-dd". */
  de: string | null
  ate: string | null
}

export type Aprovacao = { observacaoInterna: string | null }
export type Recusa = { motivo: string; observacaoInterna: string | null }
