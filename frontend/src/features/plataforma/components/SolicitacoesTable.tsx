import { MailWarning } from 'lucide-react'
import type { ReactNode } from 'react'
import { RecordLink, RecordRow } from '@/shared/components/common/RecordRow'
import { TableSkeletonRows } from '@/shared/components/common/TableStates'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/components/ui/table'
import { formatCnpj } from '@/shared/lib/cnpj'
import { formatDate, formatDateTime } from '@/shared/lib/format'
import type { SolicitacaoResumo } from '../types/solicitacao'
import { SolicitacaoStatusBadge } from './SolicitacaoStatusBadge'

export const SOLICITACOES_COLUMNS = 4

/** A linha inteira abre o painel de análise; o nome da empresa é o link equivalente para o teclado. */
export function SolicitacoesTable({ solicitacoes, loading, message, abrirHref }: {
  solicitacoes: SolicitacaoResumo[] | undefined
  loading: boolean
  message?: ReactNode
  abrirHref: (id: string) => string
}) {
  return (
    <Table aria-label="Solicitações de cadastro" aria-busy={loading || undefined} className="min-w-3xl table-fixed">
      <TableHeader><TableRow className="hover:bg-transparent">
        <TableHead className="text-muted-foreground">Empresa</TableHead>
        <TableHead className="w-[32%] text-muted-foreground">Responsável</TableHead>
        <TableHead className="w-40 text-muted-foreground">Status</TableHead>
        <TableHead className="w-36 text-muted-foreground">Recebida em</TableHead>
      </TableRow></TableHeader>
      <TableBody>
        {loading ? <TableSkeletonRows columns={SOLICITACOES_COLUMNS} /> : null}
        {!loading && message}
        {!loading && solicitacoes?.map((solicitacao) => (
          <RecordRow key={solicitacao.id} href={abrirHref(solicitacao.id)} recordId={solicitacao.id}>
            <TableCell>
              <RecordLink href={abrirHref(solicitacao.id)} title={solicitacao.razaoSocial}>{solicitacao.razaoSocial}</RecordLink>
              <span className="block truncate text-xs text-muted-foreground tabular-nums">{formatCnpj(solicitacao.cnpj)}</span>
            </TableCell>
            <TableCell>
              <span className="block truncate" title={solicitacao.responsavelNome}>{solicitacao.responsavelNome}</span>
              <span className="block truncate text-xs text-muted-foreground" title={solicitacao.responsavelEmail}>
                {solicitacao.responsavelEmail}
              </span>
            </TableCell>
            <TableCell>
              <SolicitacaoStatusBadge status={solicitacao.status} />
              {solicitacao.emailPendente ? (
                <span className="mt-0.5 flex items-center gap-1 text-xs text-muted-foreground">
                  <MailWarning aria-hidden="true" className="size-3.5 text-warning" />
                  E-mail não enviado
                </span>
              ) : null}
            </TableCell>
            <TableCell className="text-muted-foreground tabular-nums">
              <time dateTime={solicitacao.criadaEm} title={formatDateTime(solicitacao.criadaEm)}>{formatDate(solicitacao.criadaEm)}</time>
            </TableCell>
          </RecordRow>
        ))}
      </TableBody>
    </Table>
  )
}
