import { useEffect, useRef } from 'react'

/*
 * Realce breve da linha recém-criada ou editada: ao fechar o painel, o olho acha o registro na lista.
 * O formulário marca o id depois de salvar; a linha com esse id toca o realce uma vez ao aparecer.
 * Só cor, sem deslocamento: vale também com movimento reduzido. O foco não muda.
 */
const VALIDADE_MS = 5_000

let pendente: { id: string; ate: number } | null = null

export function destacarRegistro(id: string) {
  pendente = { id, ate: Date.now() + VALIDADE_MS }
}

export function useDestaqueRegistro<T extends HTMLElement>(id: string) {
  const ref = useRef<T>(null)

  // Sem dependências de propósito: a lista re-renderiza quando a consulta volta ou o painel fecha.
  useEffect(() => {
    if (!pendente || pendente.id !== id || Date.now() > pendente.ate || !ref.current) return
    pendente = null
    const cor = 'color-mix(in oklch, var(--primary) 16%, transparent)'
    ref.current.animate(
      [{ backgroundColor: cor }, { backgroundColor: cor, offset: 0.35 }, { backgroundColor: 'transparent' }],
      { duration: 1600, easing: 'ease-out' },
    )
  })

  return ref
}
