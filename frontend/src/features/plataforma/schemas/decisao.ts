import { z } from 'zod'

const observacaoInterna = z.string().trim().max(1000, 'Use no máximo 1000 caracteres.')

export const aprovacaoSchema = z.object({ observacaoInterna })

// O motivo vai no e-mail ao solicitante: precisa ser claro o bastante para ele entender e agir.
export const recusaSchema = z.object({
  motivo: z.string().trim()
    .min(10, 'Explique o motivo em pelo menos 10 caracteres. Ele vai no e-mail ao solicitante.')
    .max(500, 'Use no máximo 500 caracteres.'),
  observacaoInterna,
})

export type AprovacaoValues = z.infer<typeof aprovacaoSchema>
export type RecusaValues = z.infer<typeof recusaSchema>
