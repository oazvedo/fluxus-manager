import type { ReactNode } from 'react'
import { RecordLink, RecordRow } from '@/shared/components/common/RecordRow'
import { StatusBadge } from '@/shared/components/common/StatusBadge'
import { TableSkeletonRows } from '@/shared/components/common/TableStates'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/components/ui/table'
import { formatDate, formatDateTime } from '@/shared/lib/format'
import type { Usuario } from '../types/usuario'
import { UsuarioActions } from './UsuarioActions'

export const USUARIOS_COLUMNS = 5

type UsuariosTableProps = {
  usuarios: Usuario[] | undefined
  loading: boolean
  /** Conteúdo do corpo quando não há linhas (vazio ou erro). */
  message?: ReactNode
  editHref: (id: string) => string
}

export function UsuariosTable({ usuarios, loading, message, editHref }: UsuariosTableProps) {
  return (
    // Larguras fixas: a tabela não "pula" quando um status ou data muda. Em telas estreitas rola na horizontal.
    <Table aria-label="Usuários" aria-busy={loading || undefined} className="min-w-2xl table-fixed">
      <TableHeader>
        <TableRow className="hover:bg-transparent">
          <TableHead className="text-muted-foreground">Nome</TableHead>
          <TableHead className="w-72 text-muted-foreground">E-mail</TableHead>
          <TableHead className="w-28 text-muted-foreground">Status</TableHead>
          <TableHead className="w-40 text-muted-foreground">Criado em</TableHead>
          <TableHead className="w-12">
            <span className="sr-only">Ações</span>
          </TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {loading ? <TableSkeletonRows columns={USUARIOS_COLUMNS} /> : null}
        {!loading && message}
        {!loading &&
          usuarios?.map((usuario) => {
            const href = editHref(usuario.id)

            return (
              <RecordRow key={usuario.id} href={href} recordId={usuario.id}>
                <TableCell>
                  <RecordLink href={href} title={usuario.nome}>
                    {usuario.nome}
                  </RecordLink>
                </TableCell>
                <TableCell className="truncate text-muted-foreground" title={usuario.email}>
                  {usuario.email}
                </TableCell>
                <TableCell>
                  <StatusBadge active={usuario.ativo} labels={{ active: 'Ativo', inactive: 'Inativo' }} />
                </TableCell>
                <TableCell className="text-muted-foreground tabular-nums">
                  <time dateTime={usuario.criadoEm} title={formatDateTime(usuario.criadoEm)}>
                    {formatDate(usuario.criadoEm)}
                  </time>
                </TableCell>
                <TableCell className="text-right">
                  <UsuarioActions usuario={usuario} editHref={href} />
                </TableCell>
              </RecordRow>
            )
          })}
      </TableBody>
    </Table>
  )
}
