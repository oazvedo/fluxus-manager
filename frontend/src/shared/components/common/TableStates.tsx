import type { ReactNode } from 'react'
import { Skeleton } from '@/shared/components/ui/skeleton'
import { TableCell, TableRow } from '@/shared/components/ui/table'

/** Linhas fantasmas enquanto a página carrega, com a mesma altura das linhas reais. Leitores de tela ouvem só "Carregando…". */
export function TableSkeletonRows({ columns, rows = 8 }: { columns: number; rows?: number }) {
  return (
    <>
      <TableRow className="border-0 hover:bg-transparent">
        <TableCell colSpan={columns} className="p-0">
          <span role="status" className="sr-only">
            Carregando…
          </span>
        </TableCell>
      </TableRow>
      {Array.from({ length: rows }, (_, row) => (
        <TableRow key={row} aria-hidden="true" className="h-12 hover:bg-transparent">
          {Array.from({ length: columns }, (_, column) => (
            <TableCell key={column}>
              <Skeleton className={column === 0 ? 'h-4 w-48' : column === columns - 1 ? 'ml-auto size-6' : 'h-4 w-24'} />
            </TableCell>
          ))}
        </TableRow>
      ))}
    </>
  )
}

/** Mensagem que ocupa o corpo da tabela: vazio ou erro, com uma próxima ação. Erros são anunciados. */
export function TableMessage({
  columns,
  title,
  description,
  action,
  tone = 'empty',
}: {
  columns: number
  title: string
  description: string
  action?: ReactNode
  tone?: 'empty' | 'error'
}) {
  return (
    <TableRow className="hover:bg-transparent">
      <TableCell colSpan={columns} className="py-16 whitespace-normal">
        <div className="mx-auto flex max-w-sm flex-col items-center gap-1 text-center">
          <div role={tone === 'error' ? 'alert' : undefined} className="space-y-1">
            <p className="font-medium text-balance">{title}</p>
            <p className="text-sm text-pretty text-muted-foreground">{description}</p>
          </div>
          {action ? <div className="mt-4">{action}</div> : null}
        </div>
      </TableCell>
    </TableRow>
  )
}
