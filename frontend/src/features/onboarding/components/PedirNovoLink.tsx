import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { problemMessage } from '@/core/api/problem'
import { FormNotice } from '@/features/auth/components/AuthShell'
import { SubmitButton } from '@/shared/components/common/SubmitButton'
import { Field, FieldError, FieldLabel } from '@/shared/components/ui/field'
import { Input } from '@/shared/components/ui/input'
import { pressable } from '@/shared/lib/motion'
import { useReenviarVerificacao } from '../hooks/use-solicitacao'
import { reenvioSchema, type ReenvioValues } from '../schemas/solicitacao'

/** Link vencido ou perdido: pede outro pelo e-mail do responsável. A resposta não diz se há solicitação. */
export function PedirNovoLink() {
  const reenviar = useReenviarVerificacao()
  const [enviadoPara, setEnviadoPara] = useState<string | null>(null)
  const { register, handleSubmit, setError, formState: { errors } } = useForm<ReenvioValues>({
    resolver: zodResolver(reenvioSchema), defaultValues: { email: '' },
  })

  async function onSubmit({ email }: ReenvioValues) {
    try {
      await reenviar.mutateAsync(email)
      setEnviadoPara(email)
    } catch (error) {
      setError('root', { message: problemMessage(error) })
    }
  }

  if (enviadoPara) return (
    <FormNotice tone="success">
      <p>
        Se houver solicitação aguardando confirmação para{' '}
        <span className="font-medium break-all text-foreground">{enviadoPara}</span>, o novo link chega em instantes.
      </p>
      <p className="text-muted-foreground">Não chegou? Confira a caixa de spam.</p>
    </FormNotice>
  )

  return (
    <form noValidate onSubmit={handleSubmit(onSubmit)} className="space-y-5" aria-busy={reenviar.isPending}>
      <Field data-invalid={!!errors.email}>
        <FieldLabel htmlFor="novo-link-email">E-mail do responsável</FieldLabel>
        <Input id="novo-link-email" type="email" autoComplete="email" autoCapitalize="none" spellCheck={false}
          placeholder="nome@empresa.com.br" aria-invalid={!!errors.email}
          aria-describedby={errors.email ? 'novo-link-email-erro' : undefined} {...register('email')} />
        <FieldError id="novo-link-email-erro" errors={[errors.email]} />
      </Field>
      {errors.root && <FormNotice tone="error">{errors.root.message}</FormNotice>}
      <SubmitButton pending={reenviar.isPending} size="lg" className={`w-full ${pressable}`}>Enviar novo link</SubmitButton>
    </form>
  )
}
