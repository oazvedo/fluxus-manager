import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router'
import { isStatus } from '../lib/rotulos'
import type { FiltrosSolicitacoes } from '../types/solicitacao'

const data = (value: string | null) => (value && /^\d{4}-\d{2}-\d{2}$/.test(value) ? value : null)

/**
 * Estado da fila na URL, como nas telas de cadastro: página (?pagina), filtros (?status, ?de, ?ate)
 * e a solicitação aberta no painel (?solicitacao=<id>). Voltar fecha o painel; o link pode ser compartilhado.
 */
export function useSolicitacoesParams() {
  const [params, setParams] = useSearchParams()

  const page = Math.max(1, Number(params.get('pagina')) || 1)
  const abertaId = params.get('solicitacao')
  const statusParam = params.get('status')
  const deParam = params.get('de')
  const ateParam = params.get('ate')
  const filtros = useMemo<FiltrosSolicitacoes>(
    () => ({ status: isStatus(statusParam) ? statusParam : null, de: data(deParam), ate: data(ateParam) }),
    [statusParam, deParam, ateParam],
  )

  const update = useCallback((change: (next: URLSearchParams) => void) => setParams((current) => {
    const next = new URLSearchParams(current)
    change(next)
    return next
  }), [setParams])

  const setPage = useCallback(
    (value: number) => update((next) => (value > 1 ? next.set('pagina', String(value)) : next.delete('pagina'))),
    [update],
  )

  /** Trocar um filtro volta para a primeira página. */
  const setFiltro = useCallback((nome: keyof FiltrosSolicitacoes, valor: string | null) => update((next) => {
    if (valor) next.set(nome, valor)
    else next.delete(nome)
    next.delete('pagina')
  }), [update])

  const limparFiltros = useCallback(() => update((next) => {
    ;['status', 'de', 'ate', 'pagina'].forEach((nome) => next.delete(nome))
  }), [update])

  const fechar = useCallback(() => update((next) => next.delete('solicitacao')), [update])

  /** Link que abre o painel, para <Link>: funciona com Ctrl/Cmd+clique e botão do meio. */
  const abrirHref = useCallback((id: string) => {
    const next = new URLSearchParams(params)
    next.set('solicitacao', id)
    return `?${next.toString()}`
  }, [params])

  const filtrando = filtros.status !== null || filtros.de !== null || filtros.ate !== null
  return { page, setPage, filtros, filtrando, setFiltro, limparFiltros, abertaId, fechar, abrirHref }
}
