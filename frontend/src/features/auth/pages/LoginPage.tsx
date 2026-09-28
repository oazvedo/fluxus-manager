import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Navigate, useLocation } from 'react-router'
import { useSession } from '@/core/auth/session'
import { applyProblemToForm, getProblem, problemMessage } from '@/core/api/problem'
import { Field, FieldError, FieldLabel } from '@/shared/components/ui/field'
import { Input } from '@/shared/components/ui/input'
import { SubmitButton } from '@/shared/components/common/SubmitButton'
import { pressable } from '@/shared/lib/motion'
import { AuthLink, AuthShell, FormNotice } from '../components/AuthShell'
import { PasswordInput } from '../components/PasswordInput'
import { useLogin } from '../hooks/use-login'
import { loginSchema, type LoginValues } from '../schemas/login'

export function LoginPage() {
  const session = useSession()
  const location = useLocation()
  const login = useLogin()
  // Vindo do aceite de convite, o e-mail já chega preenchido.
  const email: unknown = location.state?.email
  const emailPreenchido = typeof email === 'string' && email.length > 0
  const { register, handleSubmit, setError, setFocus, formState: { errors } } = useForm<LoginValues>({
    resolver: zodResolver(loginSchema), defaultValues: { email: emailPreenchido ? email : '', senha: '' },
  })
  const from: unknown = location.state?.from
  const senhaRedefinida = location.state?.passwordReset === true
  const destination = typeof from === 'string' && from.startsWith('/') && !from.startsWith('//')
    && !from.startsWith('/login') ? from : '/'

  // Começa no primeiro campo que falta preencher.
  useEffect(() => { if (!session) setFocus(emailPreenchido ? 'senha' : 'email') }, [session, emailPreenchido, setFocus])

  if (session) return <Navigate to={destination} replace />

  async function onSubmit(values: LoginValues) {
    try {
      await login.mutateAsync(values)
    } catch (error) {
      if (!applyProblemToForm(error, setError, ['email', 'senha'])) {
        setError('root', { message: getProblem(error)?.status === 401
          ? 'E-mail ou senha inválidos, ou acesso indisponível. Confira os dados e tente novamente.'
          : problemMessage(error) })
      }
    }
  }

  return (
    <AuthShell title="Entrar" description="Use o e-mail com que sua equipe cadastrou você.">
      {senhaRedefinida && <FormNotice tone="success">Senha redefinida. Entre com sua nova senha.</FormNotice>}
      <form noValidate onSubmit={handleSubmit(onSubmit)} className="space-y-5" aria-busy={login.isPending}>
        <Field data-invalid={!!errors.email}>
          <FieldLabel htmlFor="login-email">E-mail</FieldLabel>
          <Input id="login-email" type="email" autoComplete="username" autoCapitalize="none" spellCheck={false}
            placeholder="nome@empresa.com.br"
            aria-invalid={!!errors.email} aria-describedby={errors.email ? 'login-email-erro' : undefined}
            {...register('email')} />
          <FieldError id="login-email-erro" errors={[errors.email]} />
        </Field>
        <Field data-invalid={!!errors.senha}>
          <FieldLabel htmlFor="login-senha">Senha</FieldLabel>
          <PasswordInput id="login-senha" autoComplete="current-password"
            aria-invalid={!!errors.senha} aria-describedby={errors.senha ? 'login-senha-erro' : undefined}
            {...register('senha')} />
          <FieldError id="login-senha-erro" errors={[errors.senha]} />
        </Field>
        {errors.root && <FormNotice tone="error">{errors.root.message}</FormNotice>}
        <SubmitButton pending={login.isPending} size="lg" className={`w-full ${pressable}`}>Entrar</SubmitButton>
      </form>
      <AuthLink to="/esqueci-senha" className="self-center">Esqueci minha senha</AuthLink>
    </AuthShell>
  )
}
