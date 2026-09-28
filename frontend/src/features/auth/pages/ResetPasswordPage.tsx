import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useLocation, useNavigate } from 'react-router'
import { getProblem, problemMessage } from '@/core/api/problem'
import { setSession } from '@/core/auth/session'
import { SubmitButton } from '@/shared/components/common/SubmitButton'
import { Field, FieldError, FieldLabel } from '@/shared/components/ui/field'
import { Input } from '@/shared/components/ui/input'
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

  if (!token || linkInvalido) return <main className="flex min-h-svh items-center justify-center px-6 py-12">
    <div className="w-full max-w-sm space-y-8">
      <Marca />
      <div className="space-y-2">
        <h1 className="text-xl font-semibold tracking-tight">Link inválido ou expirado</h1>
        <p className="text-sm text-pretty text-muted-foreground">
          Este link pode ter expirado ou já ter sido usado. Solicite um novo para redefinir sua senha.
        </p>
      </div>
      <Link to="/esqueci-senha" className="block text-center text-sm underline-offset-4 hover:underline">Solicitar outro link</Link>
      <Link to="/login" className="block text-center text-sm text-muted-foreground underline-offset-4 hover:text-foreground hover:underline">
        Voltar para o login
      </Link>
    </div>
  </main>

  return <main className="flex min-h-svh items-center justify-center px-6 py-12">
    <div className="w-full max-w-sm space-y-8">
      <Marca />
      <div className="space-y-2">
        <h1 className="text-xl font-semibold tracking-tight">Criar uma nova senha</h1>
        <p className="text-sm text-pretty text-muted-foreground">Escolha uma senha com pelo menos 8 caracteres, incluindo letras e números.</p>
      </div>
      <form noValidate onSubmit={handleSubmit(onSubmit)} className="space-y-5" aria-busy={isSubmitting}>
        <Field data-invalid={!!errors.novaSenha}>
          <FieldLabel htmlFor="redefinir-nova-senha">Nova senha</FieldLabel>
          <Input id="redefinir-nova-senha" type="password" autoComplete="new-password"
            aria-invalid={!!errors.novaSenha} aria-describedby={errors.novaSenha ? 'redefinir-nova-senha-erro' : 'redefinir-ajuda'}
            {...register('novaSenha')} />
          <p id="redefinir-ajuda" className="text-xs text-muted-foreground">Ao menos 8 caracteres, com letras e números.</p>
          <FieldError id="redefinir-nova-senha-erro" errors={[errors.novaSenha]} />
        </Field>
        <Field data-invalid={!!errors.confirmacao}>
          <FieldLabel htmlFor="redefinir-confirmacao">Repita a nova senha</FieldLabel>
          <Input id="redefinir-confirmacao" type="password" autoComplete="new-password"
            aria-invalid={!!errors.confirmacao} aria-describedby={errors.confirmacao ? 'redefinir-confirmacao-erro' : undefined}
            {...register('confirmacao')} />
          <FieldError id="redefinir-confirmacao-erro" errors={[errors.confirmacao]} />
        </Field>
        {errors.root && <p role="alert" className="text-sm text-destructive">{errors.root.message}</p>}
        <SubmitButton pending={isSubmitting} className="w-full">Redefinir senha</SubmitButton>
      </form>
      <Link to="/login" className="block text-center text-sm text-muted-foreground underline-offset-4 hover:text-foreground hover:underline">
        Voltar para o login
      </Link>
    </div>
  </main>
}

function Marca() {
  return <div className="flex items-center gap-3 font-semibold tracking-tight">
    <span aria-hidden="true" className="flex size-8 items-center justify-center rounded-lg bg-foreground text-background">F</span>
    FluxusManager
  </div>
}
