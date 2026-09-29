import { useLocation } from 'react-router'
import { problemMessage } from '@/core/api/problem'
import { AuthLink, AuthShell, FormNotice } from '@/features/auth/components/AuthShell'
import { Button } from '@/shared/components/ui/button'
import { Skeleton } from '@/shared/components/ui/skeleton'
import { pressable } from '@/shared/lib/motion'
import { LinhaDoTempo } from '../components/LinhaDoTempo'
import { useAcompanhamento } from '../hooks/use-solicitacao'
import { falhaDoLink, tokenDoLink } from '../lib/link'
import type { Acompanhamento } from '../types/solicitacao'

/** Acompanhamento pelo link do e-mail: só leitura, sem conta. O link não altera nem aprova nada. */
export function AcompanharSolicitacaoPage() {
  const token = tokenDoLink(useLocation().search)
  const consulta = useAcompanhamento(token)
  const falha = !token ? 'invalido' : consulta.isError ? falhaDoLink(consulta.error) : null

  if (falha === 'invalido') return (
    <AuthShell title="Link de acompanhamento inválido"
      description="Abra de novo o link do e-mail. Se você copiou o endereço, confira se ele veio inteiro.">
      <AuthLink to="/solicitar-cadastro" className="self-center">Fazer uma nova solicitação</AuthLink>
    </AuthShell>
  )

  if (falha === 'expirado') return (
    <AuthShell title="Link de acompanhamento expirado"
      description="Por segurança, este link tem prazo. O e-mail com a resposta da análise traz um link novo.">
      <AuthLink to="/login" className="self-center">Ir para o login</AuthLink>
    </AuthShell>
  )

  if (falha === 'erro') return (
    <AuthShell title="Acompanhe a solicitação">
      <FormNotice tone="error">{problemMessage(consulta.error)}</FormNotice>
      <Button variant="outline" size="lg" className={`w-full ${pressable}`} disabled={consulta.isFetching}
        onClick={() => consulta.refetch()}>
        Tentar de novo
      </Button>
    </AuthShell>
  )

  if (!consulta.data) return (
    <AuthShell title="Acompanhe a solicitação">
      <div role="status" aria-label="Carregando solicitação…" className="space-y-5">
        {Array.from({ length: 4 }, (_, item) => (
          <div key={item} aria-hidden="true" className="flex gap-3">
            <Skeleton className="size-6 rounded-full" />
            <div className="flex-1 space-y-2 pt-1"><Skeleton className="h-4 w-40" /><Skeleton className="h-3 w-28" /></div>
          </div>
        ))}
      </div>
    </AuthShell>
  )

  const acompanhamento = consulta.data
  return (
    <AuthShell title="Acompanhe a solicitação" description={acompanhamento.razaoSocial}>
      <Resumo acompanhamento={acompanhamento} />
      <LinhaDoTempo acompanhamento={acompanhamento} />
    </AuthShell>
  )
}

/** O que a pessoa precisa fazer agora, ou o desfecho. Recusa em tom neutro: não é erro dela. */
function Resumo({ acompanhamento }: { acompanhamento: Acompanhamento }) {
  switch (acompanhamento.status) {
    case 'AguardandoVerificacao':
      return <FormNotice tone="info">
        <p>Falta confirmar o e-mail do responsável.</p>
        <p className="text-muted-foreground">Abra o link de confirmação que enviamos. A análise começa depois disso.</p>
      </FormNotice>
    case 'PendenteAnalise':
      return <FormNotice tone="info">
        <p>A equipe Fluxus está analisando o pedido.</p>
        <p className="text-muted-foreground">Você recebe a resposta por e-mail. Não é preciso fazer nada agora.</p>
      </FormNotice>
    case 'Aprovada':
      return <div className="space-y-3">
        <FormNotice tone="success">
          <p>Solicitação aprovada. A empresa já está cadastrada.</p>
          <p className="text-muted-foreground">
            Enviamos um convite ao e-mail do responsável. Aceite o convite para criar a senha e entrar.
          </p>
        </FormNotice>
        <AuthLink to="/login">Ir para o login</AuthLink>
      </div>
    case 'Recusada':
      return <div role="status" className="space-y-2 rounded-lg border bg-muted/60 px-3 py-2.5 text-sm">
        <p className="font-medium">Solicitação recusada</p>
        {acompanhamento.motivoRecusa ? (
          <blockquote className="text-pretty whitespace-pre-line text-muted-foreground">{acompanhamento.motivoRecusa}</blockquote>
        ) : null}
        <p className="text-muted-foreground">Se os dados mudarem, você pode fazer uma nova solicitação.</p>
      </div>
  }
}
