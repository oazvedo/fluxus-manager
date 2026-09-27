import type { ReactNode } from 'react'
import { Skeleton } from '@/shared/components/ui/skeleton'
import { TableCell, TableRow } from '@/shared/components/ui/table'

/** Linhas fantasmas enquanto a página carrega, com a mesma altura das linhas reais. */
export function TableSkeletonRows({ columns, rows = 8 }: { columns: number; rows?: number }) {
  return Array.from({ length: rows }, (_, row) => (
    <TableRow key={row} aria-hidden="true" className="h-12 hover:bg-transparent">
      {Array.from({ length: columns }, (_, column) => (
        <TableCell key={column}>
          <Skeleton className={column === 0 ? 'h-4 w-48' : 'h-4 w-24'} />
        </TableCell>
      ))}
    </TableRow>
  ))
}

/** Mensagem que ocupa o corpo da tabela: vazio ou erro, com uma próxima ação. */
export function TableMessage({
  columns,
  title,
  description,
  action,
}: {
  columns: number
  title: string
  description: string
  action?: ReactNode
}) {
  return (
    <TableRow className="hover:bg-transparent">
      <TableCell colSpan={columns} className="py-16 whitespace-normal">
        <div className="mx-auto flex max-w-sm flex-col items-center gap-1 text-center">
          <p className="font-medium">{title}</p>
          <p className="text-sm text-pretty text-muted-foreground">{description}</p>
          {action ? <div className="mt-4">{action}</div> : null}
        </div>
      </TableCell>
    </TableRow>
  )
}
