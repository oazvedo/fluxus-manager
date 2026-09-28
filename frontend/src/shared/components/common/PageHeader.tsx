import type { ReactNode } from 'react'

type PageHeaderProps = {
  title: string
  /** Complemento discreto ao lado do título, ex.: "57 empresas". */
  meta?: ReactNode
  /** Uma frase sobre o que a tela guarda, abaixo do título. */
  description?: string
  /** Ação principal da tela, alinhada à direita. */
  action?: ReactNode
}

export function PageHeader({ title, meta, description, action }: PageHeaderProps) {
  return (
    <div className="flex flex-wrap items-end justify-between gap-x-6 gap-y-3">
      <div className="min-w-0 space-y-1">
        <div className="flex items-baseline gap-3">
          <h1 className="text-xl font-semibold tracking-tight">{title}</h1>
          {meta ? <p className="text-sm text-muted-foreground tabular-nums">{meta}</p> : null}
        </div>
        {description ? <p className="max-w-prose text-sm text-pretty text-muted-foreground">{description}</p> : null}
      </div>
      {action}
    </div>
  )
}
