import { useEffect, useRef } from 'react'
import { useLocation, useNavigate } from 'react-router'
import { problemMessage } from '@/core/api/problem'
import { AuthLink, AuthShell, FormNotice } from '@/features/auth/components/AuthShell'
import { SubmitButton } from '@/shared/components/common/SubmitButton'
import { pressable } from '@/shared/lib/motion'
import { PedirNovoLink } from '../components/PedirNovoLink'
import { useVerificarEmail } from '../hooks/use-solicitacao'
import { falhaDoLink, tokenDoLink } from '../lib/link'

/**
 * Link de confirmação do e-mail. Confirmar exige um clique: leitores de link de antivírus abrem a página sozinhos
 * e não podem gastar o token de uso único no lugar da pessoa.
 */
export function VerificarEmailPage() {
  const location = useLocation()
  const navigate = useNavigate()
  const token = tokenDoLink(location.search)
  const verificar = useVerificarEmail()
  const titulo = useRef<HTMLHeadingElement>(null)
  const concluido = verificar.isSuccess || verificar.isError

  // O conteúdo troca sem navegação: o foco acompanha o novo título.
  useEffect(() => { if (concluido) titulo.current?.focus() }, [concluido])

  function confirmar() {
    if (!token) return
    // Usado, o token não serve mais: sai da URL e do histórico.
    verificar.mutate(token, { onSuccess: () => navigate(location.pathname, { replace: true }) })
  }

  if (verificar.isSuccess) return (
    <AuthShell title="E-mail confirmado" headingRef={titulo}>
      <FormNotice tone="success">
        <p>A solicitação foi para a fila de análise da equipe Fluxus.</p>
        <p className="text-muted-foreground">
          Enviamos para o seu e-mail um link para acompanhar. A resposta também chega por e-mail.
        </p>
      </FormNotice>
      <AuthLink to="/login" className="self-center">Ir para o login</AuthLink>
    </AuthShell>
  )

  if (!token || (verificar.isError && falhaDoLink(verificar.error) === 'invalido')) return (
    <AuthShell
      title="Link inválido ou já usado"
      headingRef={titulo}
      description="Se você já confirmou, a solicitação está em análise e o link de acompanhamento está no seu e-mail. Se ainda não, peça um link novo."
    >
      <PedirNovoLink />
      <AuthLink to="/solicitar-cadastro" className="self-center">Fazer uma nova solicitação</AuthLink>
    </AuthShell>
  )

  if (verificar.isError && falhaDoLink(verificar.error) === 'expirado') return (
    <AuthShell title="Link expirado" headingRef={titulo}
      description="O prazo para confirmar este e-mail terminou. Informe o e-mail do responsável para receber outro link.">
      <PedirNovoLink />
    </AuthShell>
  )

  return (
    <AuthShell title="Confirme seu e-mail" headingRef={titulo}
      description="Confirme para enviar a solicitação de cadastro para análise.">
      {verificar.isError && <FormNotice tone="error">{problemMessage(verificar.error)}</FormNotice>}
      <SubmitButton type="button" pending={verificar.isPending} size="lg" className={`w-full ${pressable}`} onClick={confirmar}>
        {verificar.isError ? 'Tentar de novo' : 'Confirmar e-mail'}
      </SubmitButton>
    </AuthShell>
  )
}
