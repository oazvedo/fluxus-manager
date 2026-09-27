import { cn } from '@/shared/lib/utils'
import type { ConviteStatus } from '../types/convite'

const pontos: Record<ConviteStatus, string> = {
  Pendente: 'bg-emerald-600 dark:bg-emerald-400',
  Aceito: 'bg-primary',
  Expirado: 'bg-muted-foreground/60',
  Cancelado: 'bg-muted-foreground/60',
}

/** Situação do convite: ponto + texto, nunca só a cor (mesmo formato do StatusBadge). */
export function ConviteStatusBadge({ status }: { status: ConviteStatus }) {
  const encerrado = status === 'Expirado' || status === 'Cancelado'
  return (
    <span className={cn('inline-flex items-center gap-2 text-sm', encerrado && 'text-muted-foreground')}>
      <span aria-hidden="true" className={cn('size-1.5 rounded-full', pontos[status])} />
      {status}
    </span>
  )
}
