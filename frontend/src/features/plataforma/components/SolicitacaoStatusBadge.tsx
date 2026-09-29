import { cn } from '@/shared/lib/utils'
import { statusRotulo } from '../lib/rotulos'
import type { SolicitacaoStatus } from '../types/solicitacao'

// Em análise pede ação da equipe (aviso); aprovada é o desfecho bom; o resto espera alguém de fora ou já terminou.
const pontos: Record<SolicitacaoStatus, string> = {
  PendenteAnalise: 'bg-warning',
  Aprovada: 'bg-success',
  AguardandoVerificacao: 'bg-muted-foreground/60',
  Recusada: 'bg-muted-foreground/60',
}

/** Ponto + texto, nunca só a cor (mesmo formato do StatusBadge e do ConviteStatusBadge). */
export function SolicitacaoStatusBadge({ status }: { status: SolicitacaoStatus }) {
  const quieto = status === 'AguardandoVerificacao' || status === 'Recusada'
  return (
    <span className={cn('inline-flex items-center gap-2 text-sm whitespace-nowrap', quieto && 'text-muted-foreground')}>
      <span aria-hidden="true" className={cn('size-1.5 shrink-0 rounded-full', pontos[status])} />
      {statusRotulo[status]}
    </span>
  )
}
