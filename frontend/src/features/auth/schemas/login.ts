import { z } from 'zod'

export const loginSchema = z.object({
  email: z.string().trim().max(254, 'Use no máximo 254 caracteres.').pipe(z.email('Informe um e-mail válido.')),
  senha: z.string().min(1, 'Informe sua senha.'),
})

export type LoginValues = z.infer<typeof loginSchema>
