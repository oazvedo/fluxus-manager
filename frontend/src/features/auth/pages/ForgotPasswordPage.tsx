import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router'
import { problemMessage } from '@/core/api/problem'
import { Field, FieldError, FieldLabel } from '@/shared/components/ui/field'
import { Input } from '@/shared/components/ui/input'
import { SubmitButton } from '@/shared/components/common/SubmitButton'
import { solicitarRedefinicaoSenha } from '../api/password-reset'
import { esqueciSenhaSchema, type EsqueciSenhaValues } from '../schemas/password-reset'

export function ForgotPasswordPage() {
  const [enviado, setEnviado] = useState(false)
  const { register, handleSubmit, setError, formState: { errors, isSubmitting } } = useForm<EsqueciSenhaValues>({
    resolver: zodResolver(esqueciSenhaSchema), defaultValues: { email: '' },
  })

  async function onSubmit(values: EsqueciSenhaValues) {
    try {
      await solicitarRedefinicaoSenha(values.email)
      setEnviado(true)
    } catch (error) {
      setError('root', { message: problemMessage(error) })
    }
  }

  return <AuthPage>
    <div className="space-y-2">
      <h1 className="text-xl font-semibold tracking-tight">Esqueci minha senha</h1>
      <p className="text-sm text-pretty text-muted-foreground">Informe seu e-mail para receber um link de redefinição.</p>
    </div>
    {enviado ? <p role="status" className="text-sm text-pretty">
      Se o e-mail estiver cadastrado, você receberá um link para redefinir a senha.
    </p> : <form noValidate onSubmit={handleSubmit(onSubmit)} className="space-y-5" aria-busy={isSubmitting}>
      <Field data-invalid={!!errors.email}>
        <FieldLabel htmlFor="recuperacao-email">E-mail</FieldLabel>
        <Input id="recuperacao-email" type="email" autoComplete="username" autoCapitalize="none"
          aria-invalid={!!errors.email} aria-describedby={errors.email ? 'recuperacao-email-erro' : undefined}
          {...register('email')} />
        <FieldError id="recuperacao-email-erro" errors={[errors.email]} />
      </Field>
      {errors.root && <p role="alert" className="text-sm text-destructive">{errors.root.message}</p>}
      <SubmitButton pending={isSubmitting} className="w-full">Enviar link</SubmitButton>
    </form>}
    <Link to="/login" className="block text-center text-sm text-muted-foreground underline-offset-4 hover:text-foreground hover:underline">
      Voltar para o login
    </Link>
  </AuthPage>
}

function AuthPage({ children }: { children: React.ReactNode }) {
  return <main className="flex min-h-svh items-center justify-center px-6 py-12">
    <div className="w-full max-w-sm space-y-8">
      <div className="flex items-center gap-3 font-semibold tracking-tight">
        <span aria-hidden="true" className="flex size-8 items-center justify-center rounded-lg bg-foreground text-background">F</span>
        FluxusManager
      </div>
      {children}
    </div>
  </main>
}
