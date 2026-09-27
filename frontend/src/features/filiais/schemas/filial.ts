import { z } from 'zod'
import { isValidCnpj } from '@/shared/lib/cnpj'

export const filialFormSchema = z.object({
  nome: z.string().trim().min(1, 'Informe o nome da filial.').max(150, 'Use no máximo 150 caracteres.'),
  cnpj: z.string().refine(isValidCnpj, 'Informe um CNPJ válido, com 14 caracteres. Pode ser com ou sem pontuação.'),
  endereco: z.string().trim().min(1, 'Informe o endereço.').max(300, 'Use no máximo 300 caracteres.'),
})
export const editarFilialFormSchema = filialFormSchema.omit({ cnpj: true })
export type FilialFormValues = z.infer<typeof filialFormSchema>
