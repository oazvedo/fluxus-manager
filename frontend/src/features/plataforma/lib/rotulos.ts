import type { EntregaStatus, SolicitacaoStatus } from '../types/solicitacao'

export const statusRotulo: Record<SolicitacaoStatus, string> = {
  AguardandoVerificacao: 'Aguardando e-mail',
  PendenteAnalise: 'Em análise',
  Aprovada: 'Aprovada',
  Recusada: 'Recusada',
}

/** Ordem do filtro: a fila de trabalho primeiro. */
export const statusOrdem: SolicitacaoStatus[] = ['PendenteAnalise', 'AguardandoVerificacao', 'Aprovada', 'Recusada']

export function isStatus(value: string | null): value is SolicitacaoStatus {
  return value !== null && value in statusRotulo
}

export const entregaRotulo: Record<EntregaStatus, string> = { Pendente: 'Na fila', Enviado: 'Enviado', Falhou: 'Falhou' }

const tipos: Record<string, string> = {
  Verificacao: 'Confirmação de e-mail',
  Acompanhamento: 'Link de acompanhamento',
  Aprovacao: 'Aprovação e convite',
  Recusa: 'Recusa',
}

const eventos: Record<string, string> = {
  Criada: 'Solicitação recebida',
  VerificacaoReenviada: 'Link de confirmação reenviado',
  Verificada: 'E-mail confirmado',
  Visualizada: 'Detalhe consultado',
  Aprovada: 'Aprovada, empresa criada e convite enviado',
  Recusada: 'Recusada',
  EmailsReenviados: 'E-mails reenviados',
}

/** "EventoNovo" → "Evento novo": nome legível para eventos que a tela ainda não conhece. */
function legivel(value: string) {
  const texto = value.replace(/([a-z])([A-Z])/g, '$1 $2').toLowerCase()
  return texto.charAt(0).toUpperCase() + texto.slice(1)
}

export const tipoEmailRotulo = (tipo: string) => tipos[tipo] ?? legivel(tipo)
export const eventoRotulo = (evento: string) => eventos[evento] ?? legivel(evento)
