import { z } from 'zod'

// Mesmas regras dos validators da API (CriarConviteRequestValidator e AceitarConviteRequestValidator).
export const conviteFormSchema = z.object({
  email: z.string().trim().max(254, 'Use no máximo 254 caracteres.').pipe(z.email('Informe um e-mail válido.')),
  perfilId: z.string().min(1, 'Escolha o perfil de acesso.'),
})

export type ConviteFormValues = z.infer<typeof conviteFormSchema>

/** Aceite de quem ainda não tem conta: nome e senha criam o acesso. */
export const aceiteNovoUsuarioSchema = z.object({
  nome: z.string().trim().min(1, 'Informe seu nome.').max(150, 'Use no máximo 150 caracteres.'),
  senha: z.string()
    .min(8, 'Use ao menos 8 caracteres.')
    .regex(/[A-Za-z]/, 'A senha deve conter pelo menos uma letra.')
    .regex(/[0-9]/, 'A senha deve conter pelo menos um número.'),
  confirmacao: z.string().min(1, 'Repita a senha.'),
}).refine((values) => values.senha === values.confirmacao, { path: ['confirmacao'], message: 'As senhas não conferem.' })

export type AceiteNovoUsuarioValues = z.infer<typeof aceiteNovoUsuarioSchema>
