import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Navigate, useLocation } from 'react-router'
import { useSession } from '@/core/auth/session'
import { applyProblemToForm, getProblem, problemMessage } from '@/core/api/problem'
import { Field, FieldError, FieldLabel } from '@/shared/components/ui/field'
import { Input } from '@/shared/components/ui/input'
import { SubmitButton } from '@/shared/components/common/SubmitButton'
import { useLogin } from '../hooks/use-login'
import { loginSchema, type LoginValues } from '../schemas/login'

export function LoginPage() {
  const session = useSession()
  const location = useLocation()
  const login = useLogin()
  const { register, handleSubmit, setError, formState: { errors } } = useForm<LoginValues>({
    resolver: zodResolver(loginSchema), defaultValues: { email: '', senha: '' },
  })
  const from: unknown = location.state?.from
  const destination = typeof from === 'string' && from.startsWith('/') && !from.startsWith('//')
    && !from.startsWith('/login') ? from : '/'

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
    <main className="flex min-h-svh items-center justify-center px-6 py-12">
      <div className="w-full max-w-sm space-y-8">
        <div className="flex items-center gap-3 font-semibold tracking-tight">
          <span aria-hidden="true" className="flex size-8 items-center justify-center rounded-lg bg-foreground text-background">F</span>
          FluxusManager
        </div>
        <div className="space-y-2">
          <h1 className="text-xl font-semibold tracking-tight">Entrar na sua conta</h1>
          <p className="text-sm text-pretty text-muted-foreground">Acesse as empresas e os cadastros da sua equipe.</p>
        </div>
        <form noValidate onSubmit={handleSubmit(onSubmit)} className="space-y-5" aria-busy={login.isPending}>
          <Field data-invalid={!!errors.email}>
            <FieldLabel htmlFor="login-email">E-mail</FieldLabel>
            <Input id="login-email" type="email" autoComplete="username" autoCapitalize="none"
              aria-invalid={!!errors.email} aria-describedby={errors.email ? 'login-email-erro' : undefined}
              {...register('email')} />
            <FieldError id="login-email-erro" errors={[errors.email]} />
          </Field>
          <Field data-invalid={!!errors.senha}>
            <FieldLabel htmlFor="login-senha">Senha</FieldLabel>
            <Input id="login-senha" type="password" autoComplete="current-password"
              aria-invalid={!!errors.senha} aria-describedby={errors.senha ? 'login-senha-erro' : undefined}
              {...register('senha')} />
            <FieldError id="login-senha-erro" errors={[errors.senha]} />
          </Field>
          {errors.root && <p role="alert" className="text-sm text-destructive">{errors.root.message}</p>}
          <SubmitButton pending={login.isPending} className="w-full">Entrar</SubmitButton>
        </form>
      </div>
    </main>
  )
}
