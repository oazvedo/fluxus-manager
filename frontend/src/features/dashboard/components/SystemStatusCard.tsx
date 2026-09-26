import type { ReactNode } from 'react'
import { env } from '@/core/config/env'
import { cn } from '@/shared/lib/utils'
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/card'
import { Skeleton } from '@/shared/components/ui/skeleton'
import { useApiHealth } from '../hooks/useApiHealth'

function Row({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex items-center justify-between py-2 text-sm">
      <span className="text-muted-foreground">{label}</span>
      <span className="font-medium">{children}</span>
    </div>
  )
}

export function SystemStatusCard() {
  const { data, isPending } = useApiHealth()
  const time = new Intl.DateTimeFormat('pt-BR', { timeStyle: 'short' })

  return (
    <Card>
      <CardHeader>
        <CardTitle>Status do sistema</CardTitle>
      </CardHeader>
      <CardContent className="divide-y">
        <Row label="API">
          {isPending ? (
            <Skeleton className="h-4 w-20" />
          ) : (
            <span className="inline-flex items-center gap-2">
              <span className={cn('size-2 rounded-full', data?.online ? 'bg-emerald-500' : 'bg-red-500')} />
              {data?.online ? 'Online' : 'Indisponível'}
            </span>
          )}
        </Row>
        <Row label="Tempo de resposta">
          {isPending ? <Skeleton className="h-4 w-12" /> : `${data?.latencyMs ?? '—'} ms`}
        </Row>
        <Row label="Ambiente">
          <span className="capitalize">{env.mode}</span>
        </Row>
        <Row label="Última verificação">
          {isPending || !data ? <Skeleton className="h-4 w-12" /> : time.format(data.checkedAt)}
        </Row>
      </CardContent>
    </Card>
  )
}
