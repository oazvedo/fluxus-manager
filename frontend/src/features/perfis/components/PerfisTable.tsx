import type { ReactNode } from 'react'
import { Link, useNavigate } from 'react-router'
import { StatusBadge } from '@/shared/components/common/StatusBadge'
import { TableSkeletonRows } from '@/shared/components/common/TableStates'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/components/ui/table'
import { formatDate, formatDateTime } from '@/shared/lib/format'
import type { Perfil } from '../types/perfil'
import { PerfilActions } from './PerfilActions'

export const PERFIS_COLUMNS = 5

export function PerfisTable({ perfis, loading, message, editHref }: {
  perfis: Perfil[] | undefined
  loading: boolean
  message?: ReactNode
  editHref: (id: string) => string
}) {
  const navigate = useNavigate()
  return (
    <Table className="min-w-2xl table-fixed">
      <TableHeader><TableRow className="hover:bg-transparent">
        <TableHead className="text-muted-foreground">Perfil</TableHead>
        <TableHead className="w-36 text-muted-foreground">Permissões</TableHead>
        <TableHead className="w-28 text-muted-foreground">Status</TableHead>
        <TableHead className="w-40 text-muted-foreground">Criado em</TableHead>
        <TableHead className="w-12"><span className="sr-only">Ações</span></TableHead>
      </TableRow></TableHeader>
      <TableBody>
        {loading ? <TableSkeletonRows columns={PERFIS_COLUMNS} /> : null}
        {!loading && message}
        {!loading && perfis?.map((perfil) => {
          const href = editHref(perfil.id)
          return <TableRow key={perfil.id} className="h-12 cursor-pointer" onClick={(event) => {
            if ((event.target as HTMLElement).closest('a, button, [role="menuitem"]')) return
            navigate(href)
          }}>
            <TableCell>
              <Link to={href} className="block truncate font-medium underline-offset-4 outline-none hover:underline focus-visible:underline" title={perfil.descricao ?? undefined}>{perfil.nome}</Link>
            </TableCell>
            <TableCell className="text-muted-foreground">{perfil.permissoes.length} {perfil.permissoes.length === 1 ? 'permissão' : 'permissões'}</TableCell>
            <TableCell><StatusBadge active={perfil.ativo} labels={{ active: 'Ativo', inactive: 'Inativo' }} /></TableCell>
            <TableCell className="text-muted-foreground tabular-nums"><time dateTime={perfil.criadoEm} title={formatDateTime(perfil.criadoEm)}>{formatDate(perfil.criadoEm)}</time></TableCell>
            <TableCell className="text-right"><PerfilActions perfil={perfil} editHref={href} /></TableCell>
          </TableRow>
        })}
      </TableBody>
    </Table>
  )
}
