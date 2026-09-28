import type { ReactNode } from 'react'
import { RecordLink, RecordRow } from '@/shared/components/common/RecordRow'
import { StatusBadge } from '@/shared/components/common/StatusBadge'
import { TableSkeletonRows } from '@/shared/components/common/TableStates'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/components/ui/table'
import { formatDate, formatDateTime } from '@/shared/lib/format'
import { formatCnpj } from '@/shared/lib/cnpj'
import type { Filial } from '../types/filial'
import { FilialActions } from './FilialActions'

export const FILIAIS_COLUMNS = 5

type FiliaisTableProps = {
  filiais: Filial[] | undefined
  loading: boolean
  /** Conteúdo do corpo quando não há linhas (vazio ou erro). */
  message?: ReactNode
  editHref: (id: string) => string
}

export function FiliaisTable({ filiais, loading, message, editHref }: FiliaisTableProps) {
  return (
    // Larguras fixas: a tabela não "pula" quando um status ou data muda. Em telas estreitas rola na horizontal.
    <Table aria-label="Filiais" aria-busy={loading || undefined} className="min-w-2xl table-fixed">
      <TableHeader>
        <TableRow className="hover:bg-transparent">
          <TableHead className="text-muted-foreground">Filial</TableHead>
          <TableHead className="w-48 text-muted-foreground">CNPJ</TableHead>
          <TableHead className="w-28 text-muted-foreground">Status</TableHead>
          <TableHead className="w-40 text-muted-foreground">Atualizada em</TableHead>
          <TableHead className="w-12">
            <span className="sr-only">Ações</span>
          </TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {loading ? <TableSkeletonRows columns={FILIAIS_COLUMNS} /> : null}
        {!loading && message}
        {!loading &&
          filiais?.map((filial) => {
            const href = editHref(filial.id)
            const atualizadaEm = filial.atualizadoEm ?? filial.criadoEm

            return (
              <RecordRow key={filial.id} href={href}>
                <TableCell>
                  <RecordLink href={href} title={filial.nome}>
                    {filial.nome}
                  </RecordLink>
                  <span className="block truncate text-xs text-muted-foreground" title={filial.endereco}>
                    {filial.endereco}
                  </span>
                </TableCell>
                <TableCell className="tabular-nums">{formatCnpj(filial.cnpj)}</TableCell>
                <TableCell>
                  <StatusBadge active={filial.ativo} labels={{ active: 'Ativa', inactive: 'Inativa' }} />
                </TableCell>
                <TableCell className="text-muted-foreground tabular-nums">
                  <time dateTime={atualizadaEm} title={formatDateTime(atualizadaEm)}>
                    {formatDate(atualizadaEm)}
                  </time>
                </TableCell>
                <TableCell className="text-right">
                  <FilialActions filial={filial} editHref={href} />
                </TableCell>
              </RecordRow>
            )
          })}
      </TableBody>
    </Table>
  )
}
