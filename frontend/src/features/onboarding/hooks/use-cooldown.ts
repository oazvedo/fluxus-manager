import { useCallback, useEffect, useState } from 'react'

/** Espera entre reenvios: evita clique repetido e respeita o limite de requisições da API. */
export function useCooldown(seconds: number) {
  const [restante, setRestante] = useState(0)

  useEffect(() => {
    if (restante <= 0) return
    const timer = setTimeout(() => setRestante((atual) => atual - 1), 1_000)
    return () => clearTimeout(timer)
  }, [restante])

  const iniciar = useCallback(() => setRestante(seconds), [seconds])
  return [restante, iniciar] as const
}
