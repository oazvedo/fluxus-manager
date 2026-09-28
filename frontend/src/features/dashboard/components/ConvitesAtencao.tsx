import { CircleAlert, Clock } from 'lucide-react'
import { Link } from 'react-router'
import { Skeleton } from '@/shared/components/ui/skeleton'
import { formatDateTime } from '@/shared/lib/format'
import { DIAS_AVISO_VENCIMENTO } from '../api/inicio'
import { useConvitesAtencao } from '../hooks/useInicio'
import { formatRelative } from '../lib/relative-time'
import { InicioSection } from './InicioSection'

/** Convites que vão vencer ou já venceram sem resposta: o que pede ação hoje. */
export function ConvitesAtencao() {
  const { data, isPending, isError } = useConvitesAtencao()

  // Sem acesso aos convites (ou API fora), a seção some: o Início não é lugar para erro de uma lista secundária.
  if (isError) return null

  return (
    <InicioSection id="convites-atencao" title="Convites para acompanhar" count={data?.length}>
      {isPending ? (
        <div className="space-y-2 py-2" aria-busy="true">
          <Skeleton className="h-5 w-2/3" />
          <Skeleton className="h-5 w-1/2" />
        </div>
      ) : data.length === 0 ? (
        <p className="py-3 text-sm text-muted-foreground">
          Nenhum convite vencido ou vencendo nos próximos {DIAS_AVISO_VENCIMENTO} dias.
        </p>
      ) : (
        <ul className="divide-y">
          {data.map((convite) => {
            const vencido = convite.status === 'Expirado'
            const Icon = vencido ? CircleAlert : Clock
            return (
              <li key={convite.id}>
                <Link
                  to="/convites"
                  className="-mx-2 flex items-center gap-3 rounded-md px-2 py-2.5 text-sm outline-none transition-colors hover:bg-muted/60 focus-visible:ring-2 focus-visible:ring-ring"
                >
                  <span className="min-w-0 flex-1 sm:flex sm:items-baseline sm:gap-3">
                    <span className="block truncate font-medium">{convite.email}</span>
                    <span className="block truncate text-muted-foreground">{convite.perfil}</span>
                  </span>
                  <span
                    className={
                      vencido
                        ? 'inline-flex shrink-0 items-center gap-1.5 text-destructive'
                        : 'inline-flex shrink-0 items-center gap-1.5 text-muted-foreground'
                    }
                  >
                    <Icon aria-hidden="true" className="size-3.5" />
                    <time dateTime={convite.expiraEm} title={formatDateTime(convite.expiraEm)}>
                      {vencido ? 'Venceu' : 'Vence'} {formatRelative(convite.expiraEm)}
                    </time>
                  </span>
                </Link>
              </li>
            )
          })}
        </ul>
      )}
    </InicioSection>
  )
}
