import { useMutation, useQuery } from '@tanstack/react-query'
import { acompanharSolicitacao, enviarSolicitacao, reenviarVerificacao, verificarEmail } from '../api/solicitacao'

export function useEnviarSolicitacao() {
  return useMutation({ mutationFn: enviarSolicitacao, retry: false })
}

export function useReenviarVerificacao() {
  return useMutation({ mutationFn: reenviarVerificacao, retry: false })
}

/** Consome o token: uma tentativa só. Repetir após sucesso daria "link já usado". */
export function useVerificarEmail() {
  return useMutation({ mutationFn: verificarEmail, retry: false })
}

/** Leitura pelo link do e-mail: 404/410 descrevem o link, então não se tenta de novo. */
export function useAcompanhamento(token: string | null) {
  return useQuery({
    queryKey: ['solicitacao-cadastro', 'acompanhamento', token ?? ''],
    queryFn: () => acompanharSolicitacao(token!),
    enabled: token !== null,
    retry: false,
    staleTime: 60_000,
  })
}
