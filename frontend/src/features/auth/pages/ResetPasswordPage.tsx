import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useLocation, useNavigate } from 'react-router'
import { getProblem, problemMessage } from '@/core/api/problem'
import { setSession } from '@/core/auth/session'
import { SubmitButton } from '@/shared/components/common/SubmitButton'
import { buttonVariants } from '@/shared/components/ui/button'
import { Field, FieldDescription, FieldError, FieldLabel } from '@/shared/components/ui/field'
import { cn } from '@/shared/lib/utils'
import { pressable } from '@/shared/lib/motion'
import { AuthLink, AuthShell, FormNotice } from '../components/AuthShell'
import { PasswordInput } from '../components/PasswordInput'
import { redefinirSenha } from '../api/password-reset'
import { tokenDeRedefinicao } from '../lib/reset-token'
import { redefinirSenhaSchema, type RedefinirSenhaValues } from '../schemas/password-reset'

export function ResetPasswordPage() {
  const location = useLocation()
  const navigate = useNavigate()
  const token = tokenDeRedefinicao(location.search)
  const [linkInvalido, setLinkInvalido] = useState(false)
  const { register, handleSubmit, setError, formState: { errors, isSubmitting } } = useForm<RedefinirSenhaValues>({
    resolver: zodResolver(redefinirSenhaSchema), defaultValues: { novaSenha: '', confirmacao: '' },
  })

  async function onSubmit(values: RedefinirSenhaValues) {
    if (!token) return
    try {
      await redefinirSenha(token, values.novaSenha)
      setSession(null)
      navigate('/login', { replace: true, state: { passwordReset: true } })
    } catch (error) {
      const problem = getProblem(error)
      if (problem?.status === 422) {
        setLinkInvalido(true)
        return
      }
      const validationErrors = problem?.errors?.novaSenha
      if (validationErrors?.length) {
        setError('novaSenha', { type: 'server', message: validationErrors[0] })
      } else {
        setError('root', { message: problemMessage(error) })
      }
    }
  }

  if (!token || linkInvalido) return (
    <AuthShell title="Link inválido ou expirado"
      description="Este link pode ter expirado ou já ter sido usado. Solicite um novo para redefinir sua senha.">
      <div className="flex flex-col items-center gap-3">
        <Link to="/esqueci-senha" className={cn(buttonVariants({ size: 'lg' }), 'w-full', pressable)}>
          Solicitar outro link
        </Link>
        <AuthLink to="/login">Voltar para o login</AuthLink>
      </div>
    </AuthShell>
  )

  return (
    <AuthShell title="Criar uma nova senha" description="Depois de salvar, você entra com a nova senha.">
      <form noValidate onSubmit={handleSubmit(onSubmit)} className="space-y-5" aria-busy={isSubmitting}>
        <Field data-invalid={!!errors.novaSenha}>
          <FieldLabel htmlFor="redefinir-nova-senha">Nova senha</FieldLabel>
          <PasswordInput id="redefinir-nova-senha" autoComplete="new-password" autoFocus
            aria-invalid={!!errors.novaSenha}
            aria-describedby={errors.novaSenha ? 'redefinir-nova-senha-erro redefinir-ajuda' : 'redefinir-ajuda'}
            {...register('novaSenha')} />
          <FieldError id="redefinir-nova-senha-erro" errors={[errors.novaSenha]} />
          <FieldDescription id="redefinir-ajuda">Ao menos 8 caracteres, com letras e números.</FieldDescription>
        </Field>
        <Field data-invalid={!!errors.confirmacao}>
          <FieldLabel htmlFor="redefinir-confirmacao">Repita a nova senha</FieldLabel>
          <PasswordInput id="redefinir-confirmacao" autoComplete="new-password"
            aria-invalid={!!errors.confirmacao} aria-describedby={errors.confirmacao ? 'redefinir-confirmacao-erro' : undefined}
            {...register('confirmacao')} />
          <FieldError id="redefinir-confirmacao-erro" errors={[errors.confirmacao]} />
        </Field>
        {errors.root && <FormNotice tone="error">{errors.root.message}</FormNotice>}
        <SubmitButton pending={isSubmitting} size="lg" className={`w-full ${pressable}`}>Redefinir senha</SubmitButton>
      </form>
      <AuthLink to="/login" className="self-center">Voltar para o login</AuthLink>
    </AuthShell>
  )
}
