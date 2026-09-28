import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect, useRef, useState } from 'react'
import { useForm } from 'react-hook-form'
import { problemMessage } from '@/core/api/problem'
import { Button } from '@/shared/components/ui/button'
import { Field, FieldError, FieldLabel } from '@/shared/components/ui/field'
import { Input } from '@/shared/components/ui/input'
import { SubmitButton } from '@/shared/components/common/SubmitButton'
import { pressable } from '@/shared/lib/motion'
import { AuthLink, AuthShell, FormNotice } from '../components/AuthShell'
import { solicitarRedefinicaoSenha } from '../api/password-reset'
import { esqueciSenhaSchema, type EsqueciSenhaValues } from '../schemas/password-reset'

export function ForgotPasswordPage() {
  const [enviadoPara, setEnviadoPara] = useState<string | null>(null)
  const titulo = useRef<HTMLHeadingElement>(null)
  const { register, handleSubmit, setError, setFocus, reset, formState: { errors, isSubmitting } } = useForm<EsqueciSenhaValues>({
    resolver: zodResolver(esqueciSenhaSchema), defaultValues: { email: '' },
  })

  // O formulário some ao enviar: o foco vai para o novo título em vez de se perder no documento.
  useEffect(() => {
    if (enviadoPara) titulo.current?.focus()
    else setFocus('email')
  }, [enviadoPara, setFocus])

  async function onSubmit(values: EsqueciSenhaValues) {
    try {
      await solicitarRedefinicaoSenha(values.email)
      setEnviadoPara(values.email)
    } catch (error) {
      setError('root', { message: problemMessage(error) })
    }
  }

  function usarOutroEmail() {
    reset({ email: enviadoPara ?? '' })
    setEnviadoPara(null)
  }

  if (enviadoPara) return (
    <AuthShell title="Confira seu e-mail" headingRef={titulo}>
      <FormNotice tone="success">
        <p>Se o e-mail estiver cadastrado, você receberá um link para redefinir a senha.</p>
        <p className="text-muted-foreground">
          Enviado para <span className="font-medium break-all text-foreground">{enviadoPara}</span>. Não chegou? Confira a
          caixa de spam.
        </p>
      </FormNotice>
      <div className="flex flex-col items-center gap-3">
        <Button type="button" variant="outline" size="lg" className={`w-full ${pressable}`} onClick={usarOutroEmail}>
          Usar outro e-mail
        </Button>
        <AuthLink to="/login">Voltar para o login</AuthLink>
      </div>
    </AuthShell>
  )

  return (
    <AuthShell title="Esqueci minha senha" description="Informe seu e-mail para receber um link de redefinição.">
      <form noValidate onSubmit={handleSubmit(onSubmit)} className="space-y-5" aria-busy={isSubmitting}>
        <Field data-invalid={!!errors.email}>
          <FieldLabel htmlFor="recuperacao-email">E-mail</FieldLabel>
          <Input id="recuperacao-email" type="email" autoComplete="username" autoCapitalize="none" spellCheck={false}
            placeholder="nome@empresa.com.br"
            aria-invalid={!!errors.email} aria-describedby={errors.email ? 'recuperacao-email-erro' : undefined}
            {...register('email')} />
          <FieldError id="recuperacao-email-erro" errors={[errors.email]} />
        </Field>
        {errors.root && <FormNotice tone="error">{errors.root.message}</FormNotice>}
        <SubmitButton pending={isSubmitting} size="lg" className={`w-full ${pressable}`}>Enviar link</SubmitButton>
      </form>
      <AuthLink to="/login" className="self-center">Voltar para o login</AuthLink>
    </AuthShell>
  )
}
