import { z } from 'zod'

// Mesmas regras dos validators da API; a API continua sendo a fonte da verdade.
const nome = z.string().trim().min(1, 'Informe o nome.').max(150, 'Use no máximo 150 caracteres.')

const email = z
  .string()
  .trim()
  .min(1, 'Informe o e-mail.')
  .max(254, 'Use no máximo 254 caracteres.')
  .pipe(z.email('Informe um e-mail válido, como nome@empresa.com.br.'))

const senha = z
  .string()
  .min(8, 'Use pelo menos 8 caracteres.')
  .regex(/[A-Za-z]/, 'Inclua pelo menos uma letra.')
  .regex(/[0-9]/, 'Inclua pelo menos um número.')

export const usuarioFormSchema = z.object({ nome, email, senha })

/** Na edição a senha não aparece nem é enviada: a troca de senha é um fluxo próprio. */
export const editarUsuarioFormSchema = usuarioFormSchema.extend({ senha: z.string() })

export type UsuarioFormValues = z.infer<typeof usuarioFormSchema>
