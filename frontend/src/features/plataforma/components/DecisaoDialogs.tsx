import { zodResolver } from '@hookform/resolvers/zod'
import { useRef, type ReactNode, type Ref, type RefObject } from 'react'
import { useForm, useWatch, type UseFormRegisterReturn } from 'react-hook-form'
import { applyProblemToForm, problemMessage } from '@/core/api/problem'
import { SubmitButton } from '@/shared/components/common/SubmitButton'
import {
  AlertDialog,
  AlertDialogClose,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/shared/components/ui/alert-dialog'
import { Button } from '@/shared/components/ui/button'
import { Field, FieldDescription, FieldError, FieldLabel } from '@/shared/components/ui/field'
import { Textarea } from '@/shared/components/ui/textarea'
import { notify } from '@/shared/lib/notify'
import { destacarRegistro } from '@/shared/lib/record-highlight'
import { useAprovarSolicitacao, useRecusarSolicitacao } from '../hooks/use-solicitacoes'
import { aprovacaoSchema, recusaSchema, type AprovacaoValues, type RecusaValues } from '../schemas/decisao'
import type { SolicitacaoDetalhe } from '../types/solicitacao'

type DecisaoProps = {
  solicitacao: SolicitacaoDetalhe
  open: boolean
  onOpenChange: (open: boolean) => void
  /** Depois de decidir: fecha o painel e a linha realça na fila. */
  onDecidida: () => void
}

/** Aprovar cria a empresa e manda o convite: sem volta pela tela, então pede confirmação com as consequências. */
export function AprovarDialog({ solicitacao, open, onOpenChange, onDecidida }: DecisaoProps) {
  const aprovar = useAprovarSolicitacao(solicitacao.id)
  const voltar = useRef<HTMLButtonElement>(null)
  const { register, handleSubmit, setError, reset, formState: { errors } } = useForm<AprovacaoValues>({
    resolver: zodResolver(aprovacaoSchema), defaultValues: { observacaoInterna: '' },
  })

  async function onSubmit(values: AprovacaoValues) {
    try {
      await aprovar.mutateAsync({ observacaoInterna: values.observacaoInterna || null })
      destacarRegistro(solicitacao.id)
      notify.success(`${solicitacao.razaoSocial} aprovada`, { description: `Convite enviado para ${solicitacao.responsavelEmail}.` })
      onDecidida()
    } catch (error) {
      if (!applyProblemToForm(error, setError, ['observacaoInterna'])) setError('root', { message: problemMessage(error) })
    }
  }

  return (
    <DecisaoDialog open={open} pending={aprovar.isPending} initialFocus={voltar}
      onOpenChange={(next) => { if (!next) reset(); onOpenChange(next) }}
      title={`Aprovar ${solicitacao.razaoSocial}?`}
      description={<>
        A empresa é criada com os perfis padrão e <span className="font-medium text-foreground">{solicitacao.responsavelEmail}</span>{' '}
        recebe um convite para entrar como administrador. Não dá para desfazer por aqui.
      </>}
      onSubmit={handleSubmit(onSubmit)}
      erro={errors.root?.message}
      voltarRef={voltar}
      confirmar={<SubmitButton pending={aprovar.isPending}>Aprovar e criar empresa</SubmitButton>}
    >
      <ObservacaoInterna erro={errors.observacaoInterna?.message} registro={register('observacaoInterna')} />
    </DecisaoDialog>
  )
}

/** Recusar exige motivo: ele vai por e-mail ao solicitante. A observação interna fica só no histórico. */
export function RecusarDialog({ solicitacao, open, onOpenChange, onDecidida }: DecisaoProps) {
  const recusar = useRecusarSolicitacao(solicitacao.id)
  const { register, control, handleSubmit, setError, reset, formState: { errors } } = useForm<RecusaValues>({
    resolver: zodResolver(recusaSchema), mode: 'onTouched', defaultValues: { motivo: '', observacaoInterna: '' },
  })
  const tamanho = useWatch({ control, name: 'motivo' }).trim().length

  async function onSubmit(values: RecusaValues) {
    try {
      await recusar.mutateAsync({ motivo: values.motivo, observacaoInterna: values.observacaoInterna || null })
      destacarRegistro(solicitacao.id)
      notify.success(`${solicitacao.razaoSocial} recusada`, { description: 'O solicitante recebe o motivo por e-mail.' })
      onDecidida()
    } catch (error) {
      if (!applyProblemToForm(error, setError, ['motivo', 'observacaoInterna'])) setError('root', { message: problemMessage(error) })
    }
  }

  return (
    <DecisaoDialog open={open} pending={recusar.isPending}
      onOpenChange={(next) => { if (!next) reset(); onOpenChange(next) }}
      title={`Recusar ${solicitacao.razaoSocial}?`}
      description="O solicitante recebe o motivo por e-mail. A solicitação fica registrada no histórico."
      onSubmit={handleSubmit(onSubmit)}
      erro={errors.root?.message}
      confirmar={<SubmitButton variant="destructive" pending={recusar.isPending}>Recusar solicitação</SubmitButton>}
    >
      <Field data-invalid={!!errors.motivo}>
        <FieldLabel htmlFor="recusa-motivo">Motivo para o solicitante</FieldLabel>
        <Textarea id="recusa-motivo" maxLength={500} aria-invalid={!!errors.motivo}
          aria-describedby={errors.motivo ? 'recusa-motivo-erro' : 'recusa-motivo-ajuda'} {...register('motivo')} />
        {errors.motivo ? <FieldError id="recusa-motivo-erro" errors={[errors.motivo]} /> : (
          <FieldDescription id="recusa-motivo-ajuda" className="flex justify-between gap-4">
            <span>Diga o que impede o cadastro e, se houver, o que fazer.</span>
            <span className="shrink-0 tabular-nums">{tamanho}/500</span>
          </FieldDescription>
        )}
      </Field>
      <ObservacaoInterna erro={errors.observacaoInterna?.message} registro={register('observacaoInterna')} />
    </DecisaoDialog>
  )
}

function ObservacaoInterna({ erro, registro }: { erro?: string; registro: UseFormRegisterReturn }) {
  return (
    <Field data-invalid={!!erro}>
      <FieldLabel htmlFor="decisao-observacao">
        Observação interna <span className="font-normal text-muted-foreground">(opcional)</span>
      </FieldLabel>
      <Textarea id="decisao-observacao" className="min-h-16" maxLength={1000} aria-invalid={!!erro}
        aria-describedby={erro ? 'decisao-observacao-erro' : 'decisao-observacao-ajuda'} {...registro} />
      {erro ? <FieldError id="decisao-observacao-erro">{erro}</FieldError> : (
        <FieldDescription id="decisao-observacao-ajuda">Só a equipe Fluxus vê. Fica no histórico da solicitação.</FieldDescription>
      )}
    </Field>
  )
}

/** Moldura comum: pergunta, consequência, campos e o par voltar/confirmar. Não fecha enquanto envia. */
function DecisaoDialog({ open, onOpenChange, pending, title, description, onSubmit, erro, confirmar, children, initialFocus, voltarRef }: {
  open: boolean
  onOpenChange: (open: boolean) => void
  pending: boolean
  title: string
  description: ReactNode
  onSubmit: () => void
  erro?: string
  confirmar: ReactNode
  children: ReactNode
  initialFocus?: RefObject<HTMLElement | null>
  voltarRef?: Ref<HTMLButtonElement>
}) {
  return (
    <AlertDialog open={open} onOpenChange={(next) => !pending && onOpenChange(next)}>
      <AlertDialogContent initialFocus={initialFocus} className="max-w-lg">
        <form noValidate onSubmit={onSubmit} aria-busy={pending} className="grid gap-5">
          <AlertDialogHeader>
            <AlertDialogTitle>{title}</AlertDialogTitle>
            <AlertDialogDescription>{description}</AlertDialogDescription>
          </AlertDialogHeader>
          {children}
          {erro ? <p role="alert" className="text-sm text-pretty text-destructive">{erro}</p> : null}
          <AlertDialogFooter>
            <AlertDialogClose ref={voltarRef} type="button" disabled={pending} render={<Button variant="outline" />}>Voltar</AlertDialogClose>
            {confirmar}
          </AlertDialogFooter>
        </form>
      </AlertDialogContent>
    </AlertDialog>
  )
}
