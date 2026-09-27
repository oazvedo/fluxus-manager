import { ChevronLeft, ChevronRight } from 'lucide-react'
import { Button } from '@/shared/components/ui/button'
import type { PagedResult } from '@/shared/types/paged'

const number = new Intl.NumberFormat('pt-BR')

/** "1–20 de 57" + anterior/próxima. Some quando tudo cabe numa página. */
export function TablePagination({
  result,
  onPageChange,
}: {
  result: PagedResult<unknown>
  onPageChange: (page: number) => void
}) {
  if (result.totalPages <= 1) return null

  const first = (result.page - 1) * result.pageSize + 1
  const last = first + result.items.length - 1

  return (
    <nav aria-label="Paginação" className="flex items-center justify-end gap-4">
      <p className="text-sm text-muted-foreground tabular-nums">
        {number.format(first)}–{number.format(last)} de {number.format(result.totalCount)}
      </p>
      <div className="flex gap-2">
        <Button
          variant="outline"
          size="icon"
          aria-label="Página anterior"
          disabled={!result.hasPreviousPage}
          onClick={() => onPageChange(result.page - 1)}
        >
          <ChevronLeft />
        </Button>
        <Button
          variant="outline"
          size="icon"
          aria-label="Próxima página"
          disabled={!result.hasNextPage}
          onClick={() => onPageChange(result.page + 1)}
        >
          <ChevronRight />
        </Button>
      </div>
    </nav>
  )
}
