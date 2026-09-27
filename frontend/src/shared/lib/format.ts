const date = new Intl.DateTimeFormat('pt-BR', { day: '2-digit', month: 'short', year: 'numeric' })
const dateTime = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'long', timeStyle: 'short' })

/** "27 de set. de 2026" */
export function formatDate(value: string) {
  return date.format(new Date(value))
}

/** "27 de setembro de 2026 às 14:05" (para title/tooltip). */
export function formatDateTime(value: string) {
  return dateTime.format(new Date(value))
}
