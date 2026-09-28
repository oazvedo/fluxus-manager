import { useEffect, useState } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { applyProblemToForm, getProblem, problemMessage } from '@/core/api/problem'
import { Field, FieldDescription, FieldError, FieldLabel } from '@/shared/components/ui/field'
import { PageHeader } from '@/shared/components/common/PageHeader'
import { SubmitButton } from '@/shared/components/common/SubmitButton'
import { pressable } from '@/shared/lib/motion'
import { FormNotice } from '../components/AuthShell'
import { PasswordInput } from '../components/PasswordInput'
import { trocarSenha } from '../api/password-reset'
import { trocarSenhaSchema, type TrocarSenhaValues } from '../schemas/password-reset'

export function ChangePasswordPage() {
  const [senhaAlterada, setSenhaAlterada] = useState(false)
  const { register, handleSubmit, setError, setFocus, reset, formState: { errors, isSubmitting } } = useForm<TrocarSenhaValues>({
    resolver: zodResolver(trocarSenhaSchema),
    defaultValues: { senhaAtual: '', novaSenha: '', confirmacao: '' },
  })

  useEffect(() => { if (!senhaAlterada) setFocus('senhaAtual') }, [senhaAlterada, setFocus])

  async function onSubmit(values: TrocarSenhaValues) {
    try {
      await trocarSenha(values.senhaAtual, values.novaSenha)
      reset()
      setSenhaAlterada(true)
    } catch (error) {
      const problem = getProblem(error)
      if (problem?.status === 422 && problem.detail) {
        setError('senhaAtual', { type: 'server', message: problem.detail }, { shouldFocus: true })
      } else if (!applyProblemToForm(error, setError, ['senhaAtual', 'novaSenha', 'confirmacao'])) {
        setError('root', { message: problemMessage(error) })
      }
    }
  }

  return (
    <div className="mx-auto w-full max-w-xl space-y-6">
      <PageHeader title="Trocar senha" description="Confirme sua senha atual e escolha uma nova senha para sua conta." />
      {senhaAlterada && (
        <FormNotice tone="success">
          <p>Senha alterada com sucesso.</p>
          <p className="text-muted-foreground">
            Sua sessão atual continua ativa até o token de acesso expirar. As sessões não poderão ser renovadas e pedirão
            novo login após expirarem.
          </p>
        </FormNotice>
      )}
      <form noValidate onSubmit={handleSubmit(onSubmit)} className="space-y-5" aria-busy={isSubmitting}>
        <Field data-invalid={!!errors.senhaAtual}>
          <FieldLabel htmlFor="trocar-senha-atual">Senha atual</FieldLabel>
          <PasswordInput id="trocar-senha-atual" autoComplete="current-password"
            aria-invalid={!!errors.senhaAtual} aria-describedby={errors.senhaAtual ? 'trocar-senha-atual-erro' : undefined}
            {...register('senhaAtual')} />
          <FieldError id="trocar-senha-atual-erro" errors={[errors.senhaAtual]} />
        </Field>
        <Field data-invalid={!!errors.novaSenha}>
          <FieldLabel htmlFor="trocar-senha-nova">Nova senha</FieldLabel>
          <PasswordInput id="trocar-senha-nova" autoComplete="new-password"
            aria-invalid={!!errors.novaSenha}
            aria-describedby={errors.novaSenha ? 'trocar-senha-nova-erro trocar-senha-ajuda' : 'trocar-senha-ajuda'}
            {...register('novaSenha')} />
          <FieldError id="trocar-senha-nova-erro" errors={[errors.novaSenha]} />
          <FieldDescription id="trocar-senha-ajuda">Use ao menos 8 caracteres, com letras e números.</FieldDescription>
        </Field>
        <Field data-invalid={!!errors.confirmacao}>
          <FieldLabel htmlFor="trocar-senha-confirmacao">Repita a nova senha</FieldLabel>
          <PasswordInput id="trocar-senha-confirmacao" autoComplete="new-password"
            aria-invalid={!!errors.confirmacao}
            aria-describedby={errors.confirmacao ? 'trocar-senha-confirmacao-erro' : undefined}
            {...register('confirmacao')} />
          <FieldError id="trocar-senha-confirmacao-erro" errors={[errors.confirmacao]} />
        </Field>
        {errors.root && <FormNotice tone="error">{errors.root.message}</FormNotice>}
        <SubmitButton pending={isSubmitting} size="lg" className={`w-full ${pressable}`}>
          Alterar senha
        </SubmitButton>
      </form>
    </div>
  )
}
