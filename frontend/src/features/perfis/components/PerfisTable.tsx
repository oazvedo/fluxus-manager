import type { ReactNode } from 'react'
import { RecordLink, RecordRow } from '@/shared/components/common/RecordRow'
import { StatusBadge } from '@/shared/components/common/StatusBadge'
import { TableSkeletonRows } from '@/shared/components/common/TableStates'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/components/ui/table'
import { formatDate, formatDateTime } from '@/shared/lib/format'
import type { Perfil } from '../types/perfil'
import { PerfilActions } from './PerfilActions'

export const PERFIS_COLUMNS = 5

type PerfisTableProps = {
  perfis: Perfil[] | undefined
  loading: boolean
  /** Conteúdo do corpo quando não há linhas (vazio ou erro). */
  message?: ReactNode
  editHref: (id: string) => string
}

export function PerfisTable({ perfis, loading, message, editHref }: PerfisTableProps) {
  return (
    // Larguras fixas: a tabela não "pula" quando um status ou data muda. Em telas estreitas rola na horizontal.
    <Table aria-label="Perfis" aria-busy={loading || undefined} className="min-w-2xl table-fixed">
      <TableHeader>
        <TableRow className="hover:bg-transparent">
          <TableHead className="text-muted-foreground">Perfil</TableHead>
          <TableHead className="w-36 text-right text-muted-foreground">Permissões</TableHead>
          <TableHead className="w-28 pl-6 text-muted-foreground">Status</TableHead>
          <TableHead className="w-40 text-muted-foreground">Criado em</TableHead>
          <TableHead className="w-12">
            <span className="sr-only">Ações</span>
          </TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {loading ? <TableSkeletonRows columns={PERFIS_COLUMNS} /> : null}
        {!loading && message}
        {!loading &&
          perfis?.map((perfil) => {
            const href = editHref(perfil.id)

            return (
              <RecordRow key={perfil.id} href={href} recordId={perfil.id}>
                <TableCell>
                  <RecordLink href={href} title={perfil.nome}>
                    {perfil.nome}
                  </RecordLink>
                  {perfil.descricao ? (
                    <span className="block truncate text-xs text-muted-foreground" title={perfil.descricao}>
                      {perfil.descricao}
                    </span>
                  ) : null}
                </TableCell>
                {/* Contagem alinhada à direita, em algarismos tabulares, para comparar perfis de relance. */}
                <TableCell className="text-right text-muted-foreground tabular-nums">
                  {perfil.permissoes.length} {perfil.permissoes.length === 1 ? 'permissão' : 'permissões'}
                </TableCell>
                <TableCell className="pl-6">
                  <StatusBadge active={perfil.ativo} labels={{ active: 'Ativo', inactive: 'Inativo' }} />
                </TableCell>
                <TableCell className="text-muted-foreground tabular-nums">
                  <time dateTime={perfil.criadoEm} title={formatDateTime(perfil.criadoEm)}>
                    {formatDate(perfil.criadoEm)}
                  </time>
                </TableCell>
                <TableCell className="text-right">
                  <PerfilActions perfil={perfil} editHref={href} />
                </TableCell>
              </RecordRow>
            )
          })}
      </TableBody>
    </Table>
  )
}
