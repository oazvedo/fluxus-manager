export type Filial = { id: string; nome: string; cnpj: string; endereco: string; ativo: boolean; criadoEm: string; atualizadoEm: string | null }
export type CriarFilial = { nome: string; cnpj: string; endereco: string }
export type AtualizarFilial = { nome: string; endereco: string }
