/**
 * CNPJ numérico e alfanumérico: 12 caracteres [0-9A-Z] e 2 dígitos verificadores.
 * Mesmo algoritmo do backend (Domain/Common/Cnpj.cs).
 */
export const CNPJ_LENGTH = 14
const firstWeights = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]
const secondWeights = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]
const format = /^[0-9A-Z]{12}[0-9]{2}$/

export function normalizeCnpj(value: string) {
  return value.replace(/[^0-9a-z]/gi, '').toUpperCase().slice(0, CNPJ_LENGTH)
}

function checkDigit(value: string, weights: number[]) {
  const sum = [...value].reduce((total, char, index) => total + (char.charCodeAt(0) - 48) * weights[index], 0)
  const rest = sum % 11
  return rest < 2 ? '0' : String(11 - rest)
}

export function isValidCnpj(value: string) {
  const cnpj = normalizeCnpj(value)
  if (!format.test(cnpj) || /^(.)\1+$/.test(cnpj)) return false
  const base = cnpj.slice(0, 12)
  const first = checkDigit(base, firstWeights)
  const second = checkDigit(base + first, secondWeights)
  return cnpj.endsWith(first + second)
}

export function formatCnpj(value: string) {
  const cnpj = normalizeCnpj(value)
  const parts = [cnpj.slice(0, 2), cnpj.slice(2, 5), cnpj.slice(5, 8), cnpj.slice(8, 12), cnpj.slice(12, 14)]
  let result = parts[0]
  if (parts[1]) result += `.${parts[1]}`
  if (parts[2]) result += `.${parts[2]}`
  if (parts[3]) result += `/${parts[3]}`
  if (parts[4]) result += `-${parts[4]}`
  return result
}
