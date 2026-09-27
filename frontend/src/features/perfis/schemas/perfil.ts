import { z } from 'zod'

export const perfilFormSchema = z.object({
  nome: z.string().trim().min(1, 'Informe o nome.').max(100, 'Use no máximo 100 caracteres.'),
  descricao: z.string().trim().max(250, 'Use no máximo 250 caracteres.'),
  permissoes: z.array(z.string()),
})

export type PerfilFormValues = z.infer<typeof perfilFormSchema>
