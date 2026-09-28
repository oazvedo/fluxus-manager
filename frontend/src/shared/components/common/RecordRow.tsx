import type { ReactNode } from 'react'
import { Link, useNavigate } from 'react-router'
import { TableRow } from '@/shared/components/ui/table'
import { useDestaqueRegistro } from '@/shared/lib/record-highlight'
import { cn } from '@/shared/lib/utils'

/**
 * Linha que abre a edição. Atalho de mouse: a linha inteira navega.
 * Pelo teclado, o RecordLink da primeira coluna faz o mesmo (com Ctrl/Cmd+clique e botão do meio).
 */
export function RecordRow({ href, recordId, children }: { href: string; recordId: string; children: ReactNode }) {
  const navigate = useNavigate()
  const ref = useDestaqueRegistro<HTMLTableRowElement>(recordId)

  return (
    <TableRow
      ref={ref}
      className="h-12 cursor-pointer"
      onClick={(event) => {
        if ((event.target as HTMLElement).closest('a, button, [role="menuitem"]')) return
        navigate(href)
      }}
    >
      {children}
    </TableRow>
  )
}

/** Nome do registro: link de edição com anel de foco visível para quem navega pelo teclado. */
export function RecordLink({ href, title, className, children }: { href: string; title?: string; className?: string; children: ReactNode }) {
  return (
    <Link
      to={href}
      title={title}
      className={cn(
        'block truncate rounded-sm font-medium underline-offset-4 outline-none hover:underline focus-visible:underline focus-visible:ring-2 focus-visible:ring-ring/60',
        className,
      )}
    >
      {children}
    </Link>
  )
}
