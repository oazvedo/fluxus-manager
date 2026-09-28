import type { ReactNode } from 'react'
import { TableSkeletonRows } from '@/shared/components/common/TableStates'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/components/ui/table'
import { formatDate, formatDateTime } from '@/shared/lib/format'
import type { Convite } from '../types/convite'
import { ConviteActions } from './ConviteActions'
import { ConviteStatusBadge } from './ConviteStatusBadge'

export const CONVITES_COLUMNS = 5

/** Sem edição: a linha não abre painel. As ações (reenviar, cancelar) ficam no menu. */
export function ConvitesTable({ convites, loading, message }: {
  convites: Convite[] | undefined
  loading: boolean
  message?: ReactNode
}) {
  return (
    <Table aria-label="Convites" aria-busy={loading || undefined} className="min-w-2xl table-fixed">
      <TableHeader><TableRow className="hover:bg-transparent">
        <TableHead className="text-muted-foreground">E-mail</TableHead>
        <TableHead className="w-44 text-muted-foreground">Perfil</TableHead>
        <TableHead className="w-28 text-muted-foreground">Status</TableHead>
        <TableHead className="w-40 text-muted-foreground">Validade</TableHead>
        <TableHead className="w-12"><span className="sr-only">Ações</span></TableHead>
      </TableRow></TableHeader>
      <TableBody>
        {loading ? <TableSkeletonRows columns={CONVITES_COLUMNS} /> : null}
        {!loading && message}
        {!loading && convites?.map((convite) => (
          <TableRow key={convite.id} className="h-12">
            <TableCell><span className="block truncate font-medium" title={convite.email}>{convite.email}</span></TableCell>
            <TableCell><span className="block truncate text-muted-foreground" title={convite.perfil}>{convite.perfil}</span></TableCell>
            <TableCell><ConviteStatusBadge status={convite.status} /></TableCell>
            <TableCell className="text-muted-foreground tabular-nums"><Validade convite={convite} /></TableCell>
            <TableCell className="text-right"><ConviteActions convite={convite} /></TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}

/** Pendente/expirado mostram o prazo; aceito e cancelado, quando isso aconteceu. */
function Validade({ convite }: { convite: Convite }) {
  const [rotulo, data] = convite.status === 'Aceito' && convite.aceitoEm ? ['Aceito em', convite.aceitoEm]
    : convite.status === 'Cancelado' && convite.canceladoEm ? ['Cancelado em', convite.canceladoEm]
      : [convite.status === 'Expirado' ? 'Venceu em' : 'Vence em', convite.expiraEm]
  return <span className="block truncate">
    <span className="sr-only">{rotulo} </span>
    <time dateTime={data} title={`${rotulo} ${formatDateTime(data)}`}>{formatDate(data)}</time>
  </span>
}
