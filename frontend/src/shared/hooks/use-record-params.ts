import { useCallback } from 'react'
import { useSearchParams } from 'react-router'

/**
 * Estado de uma tela de cadastro guardado na URL: página atual (?pagina=2) e o painel aberto
 * (?novo para incluir, ?editar=<id> para editar). O voltar do navegador fecha o painel e o link pode ser compartilhado.
 */
export function useRecordParams() {
  const [params, setParams] = useSearchParams()

  const page = Math.max(1, Number(params.get('pagina')) || 1)
  const editingId = params.get('editar')
  const creating = params.has('novo')

  const update = useCallback(
    (change: (next: URLSearchParams) => void, replace = false) =>
      setParams(
        (current) => {
          const next = new URLSearchParams(current)
          change(next)
          return next
        },
        { replace },
      ),
    [setParams],
  )

  const setPage = useCallback(
    (value: number) => update((next) => (value > 1 ? next.set('pagina', String(value)) : next.delete('pagina'))),
    [update],
  )

  const openCreate = useCallback(() => update((next) => next.set('novo', '')), [update])

  const closeSheet = useCallback(
    () =>
      update((next) => {
        next.delete('novo')
        next.delete('editar')
      }),
    [update],
  )

  /** Link (href) que abre a edição, para usar em <Link>: funciona com Ctrl/Cmd+clique e botão do meio. */
  const editHref = useCallback(
    (id: string) => {
      const next = new URLSearchParams(params)
      next.delete('novo')
      next.set('editar', id)
      return `?${next.toString()}`
    },
    [params],
  )

  return { page, setPage, editingId, creating, openCreate, closeSheet, editHref }
}
