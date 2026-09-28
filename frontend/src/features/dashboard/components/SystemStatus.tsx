import { env } from '@/core/config/env'
import { cn } from '@/shared/lib/utils'
import { Skeleton } from '@/shared/components/ui/skeleton'
import { useApiHealth } from '../hooks/useApiHealth'

const time = new Intl.DateTimeFormat('pt-BR', { timeStyle: 'short' })

/** Linha discreta de status no rodapé: informação de apoio, não o foco da tela. */
export function SystemStatus() {
  const { data, isPending } = useApiHealth()

  return (
    <footer className="flex flex-wrap items-center gap-x-4 gap-y-1 border-t pt-4 text-xs text-muted-foreground">
      {isPending ? (
        <Skeleton className="h-4 w-48" />
      ) : (
        <>
          <span role="status" className="inline-flex items-center gap-1.5">
            <span
              aria-hidden="true"
              className={cn('size-1.5 rounded-full', data?.online ? 'bg-success' : 'bg-destructive')}
            />
            <span className={cn(!data?.online && 'font-medium text-destructive')}>
              {data?.online ? 'API online' : 'API indisponível'}
            </span>
          </span>
          {data?.online ? <span className="tabular-nums">{data.latencyMs} ms</span> : null}
          {data ? (
            <span>
              Verificado às <time dateTime={data.checkedAt.toISOString()}>{time.format(data.checkedAt)}</time>
            </span>
          ) : null}
        </>
      )}
      <span className="capitalize">{env.mode}</span>
    </footer>
  )
}
