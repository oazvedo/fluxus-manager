import { z } from 'zod'
import { isValidCnpj } from '../lib/cnpj'

// Mesmas regras dos validators da API; a API continua sendo a fonte da verdade.
// Na edição o CNPJ vem preenchido (e válido) e não é enviado: ele não é alterável.
export const empresaFormSchema = z.object({
  razaoSocial: z.string().trim().min(1, 'Informe a razão social.').max(150, 'Use no máximo 150 caracteres.'),
  nomeFantasia: z.string().trim().max(150, 'Use no máximo 150 caracteres.'),
  cnpj: z.string().refine(isValidCnpj, 'Informe um CNPJ válido, com 14 caracteres. Pode ser com ou sem pontuação.'),
})

export type EmpresaFormValues = z.infer<typeof empresaFormSchema>
