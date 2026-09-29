/** Telefone brasileiro com DDD: 10 dígitos (fixo) ou 11 (celular). Só os dígitos são guardados e enviados. */
export function normalizeTelefone(value: string) {
  return value.replace(/\D/g, '').slice(0, 11)
}

export function isValidTelefone(value: string) {
  return /^[1-9]{2}\d{8,9}$/.test(normalizeTelefone(value))
}

/** "(11) 98765-4321" ou "(11) 3456-7890", formatando enquanto se digita. */
export function formatTelefone(value: string) {
  const digits = normalizeTelefone(value)
  if (digits.length <= 2) return digits.length ? `(${digits}` : ''
  const ddd = digits.slice(0, 2)
  const numero = digits.slice(2)
  const corte = numero.length > 8 ? 5 : 4
  return numero.length > corte ? `(${ddd}) ${numero.slice(0, corte)}-${numero.slice(corte)}` : `(${ddd}) ${numero}`
}
