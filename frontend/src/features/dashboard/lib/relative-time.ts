const rtf = new Intl.RelativeTimeFormat('pt-BR', { numeric: 'auto' })

const units: [Intl.RelativeTimeFormatUnit, number][] = [
  ['day', 24 * 60 * 60 * 1000],
  ['hour', 60 * 60 * 1000],
  ['minute', 60 * 1000],
]

/** "há 3 horas", "ontem", "em 2 dias". Abaixo de um minuto, "agora". */
export function formatRelative(value: string, agora = new Date()) {
  const diff = new Date(value).getTime() - agora.getTime()
  for (const [unit, ms] of units) {
    if (Math.abs(diff) >= ms) return rtf.format(Math.round(diff / ms), unit)
  }
  return 'agora'
}
