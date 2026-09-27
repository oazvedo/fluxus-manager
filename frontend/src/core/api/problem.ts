import axios from 'axios'
import type { FieldValues, Path, UseFormSetError } from 'react-hook-form'

/** Corpo de erro da API (RFC 9457), com os erros por campo das validações. */
export type ProblemDetails = {
  status?: number
  title?: string
  detail?: string
  errors?: Record<string, string[]>
  correlationId?: string
}

const fallbackMessage = 'Não foi possível concluir a operação. Verifique sua conexão e tente novamente.'

export function getProblem(error: unknown): ProblemDetails | null {
  if (!axios.isAxiosError(error) || !error.response) return null
  const data = error.response.data as ProblemDetails | undefined
  return { ...data, status: error.response.status }
}

export function isNotFound(error: unknown) {
  return getProblem(error)?.status === 404
}

/** Mensagem para exibir ao usuário: o detalhe da API ou uma orientação genérica quando não há resposta. */
export function problemMessage(error: unknown) {
  return getProblem(error)?.detail ?? fallbackMessage
}

/**
 * Leva os erros da API para os campos do formulário: 400 com erros por campo e 409 (duplicado) no campo informado.
 * Devolve false quando nada pôde ser associado a um campo; aí o chamador mostra a mensagem geral.
 */
export function applyProblemToForm<T extends FieldValues>(
  error: unknown,
  setError: UseFormSetError<T>,
  fields: readonly Path<T>[],
  conflictField?: Path<T>,
) {
  const problem = getProblem(error)
  if (!problem) return false

  if (problem.status === 409 && conflictField && problem.detail) {
    setError(conflictField, { type: 'server', message: problem.detail }, { shouldFocus: true })
    return true
  }

  const entries = Object.entries(problem.errors ?? {}).filter(([field]) => fields.includes(field as Path<T>))
  entries.forEach(([field, messages], index) =>
    setError(field as Path<T>, { type: 'server', message: messages[0] }, { shouldFocus: index === 0 }),
  )
  return entries.length > 0
}
