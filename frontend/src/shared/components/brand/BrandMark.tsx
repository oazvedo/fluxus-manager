import type { SVGProps } from 'react'
import { cn } from '@/shared/lib/utils'

/*
 * Símbolo Fluxus: um "F" feito de duas faixas de fluxo. A haste e a faixa de cima formam a letra; a faixa
 * do meio, destacada e na cor do acento, é o registro em movimento. As pontas cortadas em diagonal (mesmo
 * ângulo nas duas) dão a direção. Grade de 24: funciona a 16px e herda a cor do texto (currentColor).
 */
const HASTE = 'M5 4H20L17.5 7.5H8.5V20H5Z'
const FLUXO = 'M10 10.25H17L14.5 13.75H10Z'

type BrandSymbolProps = SVGProps<SVGSVGElement> & {
  /** Cor da faixa de fluxo. Padrão: o acento do tema. */
  accent?: string
}

export function BrandSymbol({ accent = 'var(--primary)', className, ...props }: BrandSymbolProps) {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false" className={cn('shrink-0', className)} {...props}>
      <path d={HASTE} fill="currentColor" />
      <path d={FLUXO} fill={accent} />
    </svg>
  )
}

/** Símbolo + nome. O nome é texto real (lido por leitor de tela e selecionável), não parte do SVG. */
export function BrandLogo({ className, accent }: { className?: string; accent?: string }) {
  return (
    <span className={cn('inline-flex items-center gap-2', className)}>
      <BrandSymbol accent={accent} className="size-5" />
      <BrandWordmark />
    </span>
  )
}

/** "Fluxus" em peso cheio, "Manager" em peso leve: o nome do produto sem repetir o peso duas vezes. */
export function BrandWordmark({ className }: { className?: string }) {
  return (
    <span className={cn('text-[0.9375rem] leading-none tracking-[-0.015em]', className)}>
      <span className="font-semibold">Fluxus</span>
      <span className="font-normal opacity-70">Manager</span>
    </span>
  )
}
