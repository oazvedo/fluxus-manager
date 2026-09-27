import type { ReactNode } from 'react'
import { Link, useNavigate } from 'react-router'
import { StatusBadge } from '@/shared/components/common/StatusBadge'
import { TableSkeletonRows } from '@/shared/components/common/TableStates'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/components/ui/table'
import { formatDate, formatDateTime } from '@/shared/lib/format'
import { formatCnpj } from '@/shared/lib/cnpj'
import type { Filial } from '../types/filial'
import { FilialActions } from './FilialActions'

export const FILIAIS_COLUMNS = 5
export function FiliaisTable({ filiais, loading, message, editHref }: { filiais?: Filial[]; loading: boolean; message?: ReactNode; editHref: (id: string) => string }) {
  const navigate = useNavigate()
  return <Table className="min-w-2xl table-fixed"><TableHeader><TableRow className="hover:bg-transparent"><TableHead className="text-muted-foreground">Filial</TableHead><TableHead className="w-48 text-muted-foreground">CNPJ</TableHead><TableHead className="w-28 text-muted-foreground">Status</TableHead><TableHead className="w-40 text-muted-foreground">Atualizada em</TableHead><TableHead className="w-12"><span className="sr-only">Ações</span></TableHead></TableRow></TableHeader><TableBody>
    {loading ? <TableSkeletonRows columns={FILIAIS_COLUMNS} /> : null}{!loading && message}
    {!loading && filiais?.map((filial) => { const href = editHref(filial.id); const updated = filial.atualizadoEm ?? filial.criadoEm; return <TableRow key={filial.id} className="h-12 cursor-pointer" onClick={(event) => { if ((event.target as HTMLElement).closest('a, button, [role="menuitem"]')) return; navigate(href) }}>
      <TableCell><Link to={href} title={filial.nome} className="block truncate font-medium underline-offset-4 outline-none hover:underline focus-visible:underline">{filial.nome}</Link><span title={filial.endereco} className="block truncate text-xs text-muted-foreground">{filial.endereco}</span></TableCell>
      <TableCell className="tabular-nums">{formatCnpj(filial.cnpj)}</TableCell><TableCell><StatusBadge active={filial.ativo} labels={{ active: 'Ativa', inactive: 'Inativa' }} /></TableCell>
      <TableCell className="text-muted-foreground tabular-nums"><time dateTime={updated} title={formatDateTime(updated)}>{formatDate(updated)}</time></TableCell><TableCell className="text-right"><FilialActions filial={filial} editHref={href} /></TableCell>
    </TableRow> })}
  </TableBody></Table>
}
