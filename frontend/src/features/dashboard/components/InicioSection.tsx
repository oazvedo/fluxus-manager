import type { ReactNode } from 'react'

type InicioSectionProps = {
  id: string
  title: string
  /** Quantidade de itens, ao lado do título. Omitida enquanto carrega. */
  count?: number
  children: ReactNode
}

/** Bloco do Início: título pequeno e a lista logo abaixo, sem caixa em volta. */
export function InicioSection({ id, title, count, children }: InicioSectionProps) {
  return (
    <section aria-labelledby={id} className="space-y-1">
      <h2 id={id} className="flex items-baseline gap-2 text-sm font-medium">
        {title}
        {count ? <span className="font-normal text-muted-foreground tabular-nums">{count}</span> : null}
      </h2>
      {children}
    </section>
  )
}
