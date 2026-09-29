import { Check, X } from 'lucide-react'
import { formatDateTime } from '@/shared/lib/format'
import { cn } from '@/shared/lib/utils'
import type { Acompanhamento } from '../types/solicitacao'
import { etapasDaSolicitacao, leitura, type Situacao } from '../lib/etapas'

export function LinhaDoTempo({ acompanhamento }: { acompanhamento: Acompanhamento }) {
  const etapas = etapasDaSolicitacao(acompanhamento)
  return (
    <ol aria-label="Etapas da solicitação" className="relative">
      {etapas.map((etapa, index) => (
        <li key={etapa.titulo} aria-current={etapa.situacao === 'atual' ? 'step' : undefined}
          className="relative flex gap-3 pb-5 last:pb-0">
          {/* Trilho entre os marcadores: sólido no que já passou. */}
          {index < etapas.length - 1 ? (
            <span aria-hidden="true" className={cn('absolute top-6 bottom-0 left-[11px] w-px',
              etapa.situacao === 'feito' ? 'bg-foreground/25' : 'bg-border')} />
          ) : null}
          <Marcador situacao={etapa.situacao} />
          <div className="min-w-0 space-y-0.5 pt-0.5">
            <p className={cn('text-sm font-medium', etapa.situacao === 'pendente' && 'text-muted-foreground')}>
              {etapa.titulo}<span className="sr-only">, {leitura[etapa.situacao]}</span>
            </p>
            {etapa.em ? (
              <p className="text-sm text-muted-foreground tabular-nums">
                <time dateTime={etapa.em}>{formatDateTime(etapa.em)}</time>
              </p>
            ) : null}
            {etapa.detalhe ? <p className="text-sm text-pretty text-muted-foreground">{etapa.detalhe}</p> : null}
          </div>
        </li>
      ))}
    </ol>
  )
}

function Marcador({ situacao }: { situacao: Situacao }) {
  const base = 'relative flex size-6 shrink-0 items-center justify-center rounded-full'
  if (situacao === 'feito') return (
    <span aria-hidden="true" className={cn(base, 'bg-foreground text-background')}><Check className="size-3.5" strokeWidth={2.5} /></span>
  )
  if (situacao === 'recusado') return (
    <span aria-hidden="true" className={cn(base, 'bg-muted text-foreground ring-1 ring-foreground/20')}><X className="size-3.5" strokeWidth={2.5} /></span>
  )
  if (situacao === 'atual') return (
    <span aria-hidden="true" className={cn(base, 'bg-background ring-2 ring-foreground')}><span className="size-2 rounded-full bg-foreground" /></span>
  )
  return <span aria-hidden="true" className={cn(base, 'bg-background ring-1 ring-border')} />
}
