import type { Acompanhamento } from '../types/solicitacao'

export type Situacao = 'feito' | 'atual' | 'pendente' | 'recusado'
export type Etapa = { titulo: string; detalhe?: string; em?: string | null; situacao: Situacao }

/** Como o leitor de tela ouve cada situação. */
export const leitura: Record<Situacao, string> = { feito: 'concluída', atual: 'em andamento', pendente: 'a seguir', recusado: 'recusada' }

/**
 * Três etapas: envio, confirmação do e-mail e análise, que termina no resultado. Sem cor de destaque
 * (reservada ao que é clicável): feito é preenchido, a etapa atual é um anel, o que vem depois é só contorno.
 */
export function etapasDaSolicitacao(a: Acompanhamento): Etapa[] {
  const verificado = a.verificadaEm !== null
  return [
    { titulo: 'Solicitação enviada', em: a.criadaEm, situacao: 'feito' },
    verificado
      ? { titulo: 'E-mail confirmado', em: a.verificadaEm, situacao: 'feito' }
      : { titulo: 'Confirmação do e-mail', detalhe: 'Abra o link que enviamos ao responsável.', situacao: 'atual' },
    a.status === 'Aprovada' ? { titulo: 'Aprovada pela equipe Fluxus', em: a.decididaEm, situacao: 'feito' }
      : a.status === 'Recusada' ? { titulo: 'Recusada pela equipe Fluxus', em: a.decididaEm, situacao: 'recusado' }
        : { titulo: 'Análise da equipe Fluxus', detalhe: verificado ? 'Em andamento. Você recebe a resposta por e-mail.' : undefined,
          situacao: verificado ? 'atual' : 'pendente' },
  ]
}
