import type { ReactNode } from 'react'
import { RecordLink, RecordRow } from '@/shared/components/common/RecordRow'
import { StatusBadge } from '@/shared/components/common/StatusBadge'
import { TableSkeletonRows } from '@/shared/components/common/TableStates'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/components/ui/table'
import { formatDate, formatDateTime } from '@/shared/lib/format'
import { formatCnpj } from '@/shared/lib/cnpj'
import type { Empresa } from '../types/empresa'
import { EmpresaActions } from './EmpresaActions'

export const EMPRESAS_COLUMNS = 5

type EmpresasTableProps = {
  empresas: Empresa[] | undefined
  loading: boolean
  /** Conteúdo do corpo quando não há linhas (vazio ou erro). */
  message?: ReactNode
  editHref: (id: string) => string
}

export function EmpresasTable({ empresas, loading, message, editHref }: EmpresasTableProps) {
  return (
    // Larguras fixas: a tabela não "pula" quando um status ou data muda. Em telas estreitas rola na horizontal.
    <Table aria-label="Empresas" aria-busy={loading || undefined} className="min-w-2xl table-fixed">
      <TableHeader>
        <TableRow className="hover:bg-transparent">
          <TableHead className="text-muted-foreground">Empresa</TableHead>
          <TableHead className="w-48 text-muted-foreground">CNPJ</TableHead>
          <TableHead className="w-28 text-muted-foreground">Status</TableHead>
          <TableHead className="w-40 text-muted-foreground">Atualizada em</TableHead>
          <TableHead className="w-12">
            <span className="sr-only">Ações</span>
          </TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {loading ? <TableSkeletonRows columns={EMPRESAS_COLUMNS} /> : null}
        {!loading && message}
        {!loading &&
          empresas?.map((empresa) => {
            const href = editHref(empresa.id)
            const atualizadaEm = empresa.atualizadoEm ?? empresa.criadoEm

            return (
              <RecordRow key={empresa.id} href={href} recordId={empresa.id}>
                <TableCell>
                  <RecordLink href={href} title={empresa.razaoSocial}>
                    {empresa.razaoSocial}
                  </RecordLink>
                  {empresa.nomeFantasia ? (
                    <span className="block truncate text-xs text-muted-foreground" title={empresa.nomeFantasia}>
                      {empresa.nomeFantasia}
                    </span>
                  ) : null}
                </TableCell>
                <TableCell className="tabular-nums">{formatCnpj(empresa.cnpj)}</TableCell>
                <TableCell>
                  <StatusBadge active={empresa.ativo} labels={{ active: 'Ativa', inactive: 'Inativa' }} />
                </TableCell>
                <TableCell className="text-muted-foreground tabular-nums">
                  <time dateTime={atualizadaEm} title={formatDateTime(atualizadaEm)}>
                    {formatDate(atualizadaEm)}
                  </time>
                </TableCell>
                <TableCell className="text-right">
                  <EmpresaActions empresa={empresa} editHref={href} />
                </TableCell>
              </RecordRow>
            )
          })}
      </TableBody>
    </Table>
  )
}
