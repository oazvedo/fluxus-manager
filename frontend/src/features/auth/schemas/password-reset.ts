import { z } from 'zod'

export const esqueciSenhaSchema = z.object({
  email: z.string().trim().max(254, 'Use no máximo 254 caracteres.').pipe(z.email('Informe um e-mail válido.')),
})

export const redefinirSenhaSchema = z.object({
  novaSenha: z.string()
    .min(8, 'Use ao menos 8 caracteres.')
    .regex(/[A-Za-z]/, 'A senha deve conter pelo menos uma letra.')
    .regex(/[0-9]/, 'A senha deve conter pelo menos um número.'),
  confirmacao: z.string().min(1, 'Repita a senha.'),
}).refine((values) => values.novaSenha === values.confirmacao, {
  path: ['confirmacao'], message: 'As senhas não conferem.',
})

export type EsqueciSenhaValues = z.infer<typeof esqueciSenhaSchema>
export type RedefinirSenhaValues = z.infer<typeof redefinirSenhaSchema>
