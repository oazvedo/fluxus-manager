import { useState, type ReactNode } from 'react'
import { isNotFound, problemMessage } from '@/core/api/problem'
import { SheetFormSkeleton, SheetLoadError } from '@/shared/components/common/SheetStates'
import { Button } from '@/shared/components/ui/button'
import { Sheet, SheetContent, SheetDescription, SheetFooter, SheetHeader, SheetTitle } from '@/shared/components/ui/sheet'
import { formatCnpj } from '@/shared/lib/cnpj'
import { formatDateTime } from '@/shared/lib/format'
import { notify } from '@/shared/lib/notify'
import { cn } from '@/shared/lib/utils'
import { formatTelefone } from '@/shared/lib/telefone'
import { useReenviarEmails, useSolicitacao } from '../hooks/use-solicitacoes'
import { entregaRotulo, eventoRotulo, tipoEmailRotulo } from '../lib/rotulos'
import type { SolicitacaoDetalhe } from '../types/solicitacao'
import { AprovarDialog, RecusarDialog } from './DecisaoDialogs'
import { SolicitacaoStatusBadge } from './SolicitacaoStatusBadge'

/** Painel de análise, aberto pela URL (?solicitacao=<id>). A fila continua visível atrás. */
export function SolicitacaoSheet({ id, onClose }: { id: string | null; onClose: () => void }) {
  return (
    <Sheet open={id !== null} onOpenChange={(next) => !next && onClose()}>
      <SheetContent className="sm:max-w-lg">
        {id ? <Conteudo key={id} id={id} onClose={onClose} /> : null}
      </SheetContent>
    </Sheet>
  )
}

function Conteudo({ id, onClose }: { id: string; onClose: () => void }) {
  const { data, error, isPending } = useSolicitacao(id)

  if (data) return <Detalhe solicitacao={data} onClose={onClose} />
  return <>
    <SheetHeader className="border-b pr-12">
      <SheetTitle>Solicitação de cadastro</SheetTitle>
      <SheetDescription className="sr-only">Dados da empresa e do responsável</SheetDescription>
    </SheetHeader>
    {isPending ? <SheetFormSkeleton label="Carregando solicitação…" fields={5} /> : (
      <SheetLoadError message={isNotFound(error) ? 'Esta solicitação não existe mais.' : problemMessage(error)} />
    )}
  </>
}

function Detalhe({ solicitacao, onClose }: { solicitacao: SolicitacaoDetalhe; onClose: () => void }) {
  const [decisao, setDecisao] = useState<'aprovar' | 'recusar' | null>(null)
  const reenviar = useReenviarEmails(solicitacao.id)
  const emEspera = solicitacao.emails.some((email) => email.status !== 'Enviado')
  const decidivel = solicitacao.status === 'PendenteAnalise'

  function reenviarEmails() {
    reenviar.mutate(undefined, {
      onSuccess: () => notify.success('E-mails reenviados'),
      onError: (error) => notify.error(error),
    })
  }

  return <>
    <SheetHeader className="border-b pr-12">
      <SheetTitle className="text-balance">{solicitacao.razaoSocial}</SheetTitle>
      <SheetDescription render={<div />} className="flex flex-wrap items-center gap-x-3 gap-y-1">
        <SolicitacaoStatusBadge status={solicitacao.status} />
        <span className="tabular-nums">Recebida em {formatDateTime(solicitacao.criadaEm)}</span>
      </SheetDescription>
    </SheetHeader>

    <div className="flex-1 space-y-8 overflow-y-auto overscroll-contain px-4 py-4">
      {solicitacao.status === 'AguardandoVerificacao' ? (
        <p className="rounded-lg bg-muted/60 px-3 py-2.5 text-sm text-pretty text-muted-foreground">
          O responsável ainda não confirmou o e-mail. A análise fica liberada depois da confirmação.
        </p>
      ) : null}

      <Secao titulo="Empresa">
        <Dados>
          <Dado rotulo="Razão social">{solicitacao.razaoSocial}</Dado>
          <Dado rotulo="Nome fantasia">{solicitacao.nomeFantasia}</Dado>
          <Dado rotulo="CNPJ" className="tabular-nums">{formatCnpj(solicitacao.cnpj)}</Dado>
        </Dados>
      </Secao>

      <Secao titulo="Responsável">
        <Dados>
          <Dado rotulo="Nome">{solicitacao.responsavelNome}</Dado>
          <Dado rotulo="E-mail" className="break-all">{solicitacao.responsavelEmail}</Dado>
          <Dado rotulo="Telefone" className="tabular-nums">
            {solicitacao.responsavelTelefone ? formatTelefone(solicitacao.responsavelTelefone) : null}
          </Dado>
          <Dado rotulo="E-mail confirmado em" className="tabular-nums">
            {solicitacao.verificadaEm ? formatDateTime(solicitacao.verificadaEm) : 'Ainda não confirmado'}
          </Dado>
        </Dados>
      </Secao>

      {solicitacao.decididaEm ? (
        <Secao titulo="Decisão">
          <Dados>
            <Dado rotulo="Decidida por">{solicitacao.decididaPor?.nome}</Dado>
            <Dado rotulo="Em" className="tabular-nums">{formatDateTime(solicitacao.decididaEm)}</Dado>
            {solicitacao.motivoRecusa ? <Dado rotulo="Motivo enviado" className="whitespace-pre-line">{solicitacao.motivoRecusa}</Dado> : null}
            <Dado rotulo="Observação interna" className="whitespace-pre-line">{solicitacao.observacaoInterna}</Dado>
          </Dados>
        </Secao>
      ) : null}

      <Secao titulo="E-mails" acao={emEspera ? (
        <Button variant="outline" size="sm" onClick={reenviarEmails} disabled={reenviar.isPending} aria-busy={reenviar.isPending || undefined}>
          Reenviar e-mails
        </Button>
      ) : null}>
        {solicitacao.emails.length === 0 ? <p className="text-sm text-muted-foreground">Nenhum e-mail enviado ainda.</p> : (
          <ul className="divide-y text-sm">
            {solicitacao.emails.map((email, index) => (
              <li key={`${email.tipo}-${index}`} className="flex items-baseline justify-between gap-4 py-2">
                <span className="min-w-0 truncate">{tipoEmailRotulo(email.tipo)}</span>
                <span className={cn('shrink-0 tabular-nums', email.status === 'Falhou' ? 'text-destructive' : 'text-muted-foreground')}>
                  {entregaRotulo[email.status]}
                  {email.tentativas > 1 ? ` · ${email.tentativas} tentativas` : ''}
                </span>
              </li>
            ))}
          </ul>
        )}
      </Secao>

      <Secao titulo="Histórico">
        <ol className="space-y-3 text-sm">
          {solicitacao.historico.map((item, index) => (
            <li key={`${item.evento}-${item.em}-${index}`} className="space-y-0.5">
              <p>{eventoRotulo(item.evento)}{item.por ? <span className="text-muted-foreground"> · {item.por.nome}</span> : null}</p>
              <time dateTime={item.em} className="block text-muted-foreground tabular-nums">{formatDateTime(item.em)}</time>
            </li>
          ))}
        </ol>
      </Secao>
    </div>

    {decidivel ? (
      <SheetFooter className="flex-row justify-end border-t">
        <Button variant="outline" onClick={() => setDecisao('recusar')}>Recusar</Button>
        <Button onClick={() => setDecisao('aprovar')}>Aprovar</Button>
      </SheetFooter>
    ) : null}

    <AprovarDialog solicitacao={solicitacao} open={decisao === 'aprovar'} onOpenChange={(open) => !open && setDecisao(null)}
      onDecidida={() => { setDecisao(null); onClose() }} />
    <RecusarDialog solicitacao={solicitacao} open={decisao === 'recusar'} onOpenChange={(open) => !open && setDecisao(null)}
      onDecidida={() => { setDecisao(null); onClose() }} />
  </>
}

/** Seção do painel: título pequeno e espaço entre grupos, sem caixas. */
function Secao({ titulo, acao, children }: { titulo: string; acao?: ReactNode; children: ReactNode }) {
  return (
    <section aria-label={titulo} className="space-y-3">
      <div className="flex min-h-7 items-center justify-between gap-4">
        <h3 className="text-xs font-medium tracking-wide text-muted-foreground uppercase">{titulo}</h3>
        {acao}
      </div>
      {children}
    </section>
  )
}

/** Pares rótulo/valor alinhados numa grade. */
function Dados({ children }: { children: ReactNode }) {
  return <dl className="grid grid-cols-[minmax(0,9rem)_minmax(0,1fr)] gap-x-4 gap-y-2 text-sm">{children}</dl>
}

/** Valor ausente aparece como "Não informado", em tom secundário. */
function Dado({ rotulo, className, children }: { rotulo: string; className?: string; children: ReactNode }) {
  const vazio = children === null || children === undefined || children === ''
  return (
    <div className="contents">
      <dt className="text-muted-foreground">{rotulo}</dt>
      <dd className={cn('min-w-0', vazio ? 'text-muted-foreground' : className)}>{vazio ? 'Não informado' : children}</dd>
    </div>
  )
}
