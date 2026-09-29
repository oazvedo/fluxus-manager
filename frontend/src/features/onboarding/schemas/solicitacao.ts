import { z } from 'zod'
import { isValidCnpj, normalizeCnpj } from '@/shared/lib/cnpj'
import { isValidTelefone, normalizeTelefone } from '@/shared/lib/telefone'
import type { NovaSolicitacao } from '../types/solicitacao'

// Mesmas regras do cadastro de empresas; a API continua sendo a fonte da verdade.
export const solicitacaoSchema = z.object({
  razaoSocial: z.string().trim().min(1, 'Informe a razão social.').max(150, 'Use no máximo 150 caracteres.'),
  nomeFantasia: z.string().trim().max(150, 'Use no máximo 150 caracteres.'),
  cnpj: z.string().refine(isValidCnpj, 'Informe um CNPJ válido, com 14 caracteres. Pode ser com ou sem pontuação.'),
  responsavelNome: z.string().trim().min(1, 'Informe seu nome.').max(150, 'Use no máximo 150 caracteres.'),
  responsavelEmail: z.string().trim().max(254, 'Use no máximo 254 caracteres.').pipe(z.email('Informe um e-mail válido.')),
  responsavelTelefone: z.string().refine((value) => value === '' || isValidTelefone(value),
    'Informe o telefone com DDD, 10 ou 11 dígitos.'),
})

export type SolicitacaoValues = z.infer<typeof solicitacaoSchema>

/** Corpo da API: opcionais vazios viram null, CNPJ e telefone só com os caracteres que contam. */
export function paraSolicitacao(values: SolicitacaoValues): NovaSolicitacao {
  return {
    razaoSocial: values.razaoSocial,
    nomeFantasia: values.nomeFantasia || null,
    cnpj: normalizeCnpj(values.cnpj),
    responsavelNome: values.responsavelNome,
    responsavelEmail: values.responsavelEmail,
    responsavelTelefone: values.responsavelTelefone ? normalizeTelefone(values.responsavelTelefone) : null,
  }
}

export const reenvioSchema = z.object({
  email: z.string().trim().max(254, 'Use no máximo 254 caracteres.').pipe(z.email('Informe um e-mail válido.')),
})

export type ReenvioValues = z.infer<typeof reenvioSchema>
