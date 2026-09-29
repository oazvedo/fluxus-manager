import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect, useRef, useState, type ComponentProps, type ReactNode } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { applyProblemToForm, problemMessage } from '@/core/api/problem'
import { AuthLink, AuthShell, FormNotice } from '@/features/auth/components/AuthShell'
import { SubmitButton } from '@/shared/components/common/SubmitButton'
import { Button } from '@/shared/components/ui/button'
import { Field, FieldDescription, FieldError, FieldLabel, FieldLegend, FieldSet } from '@/shared/components/ui/field'
import { Input } from '@/shared/components/ui/input'
import { formatCnpj, normalizeCnpj } from '@/shared/lib/cnpj'
import { pressable } from '@/shared/lib/motion'
import { ReenviarLink } from '../components/ReenviarLink'
import { useEnviarSolicitacao } from '../hooks/use-solicitacao'
import { formatTelefone, normalizeTelefone } from '@/shared/lib/telefone'
import { paraSolicitacao, solicitacaoSchema, type SolicitacaoValues } from '../schemas/solicitacao'

const campos = ['razaoSocial', 'nomeFantasia', 'cnpj', 'responsavelNome', 'responsavelEmail', 'responsavelTelefone'] as const
type Campo = (typeof campos)[number]

/**
 * Pedido público de cadastro. Nada é criado agora: a equipe Fluxus analisa depois que o e-mail for confirmado.
 * A confirmação é neutra de propósito: não revela se o CNPJ ou o e-mail já existem.
 */
export function SolicitarCadastroPage() {
  const [enviadoPara, setEnviadoPara] = useState<string | null>(null)
  const titulo = useRef<HTMLHeadingElement>(null)
  const enviar = useEnviarSolicitacao()
  const { register, control, handleSubmit, setError, setFocus, formState: { errors } } = useForm<SolicitacaoValues>({
    resolver: zodResolver(solicitacaoSchema),
    // Valida ao sair do campo e, a partir daí, enquanto se digita.
    mode: 'onTouched',
    defaultValues: { razaoSocial: '', nomeFantasia: '', cnpj: '', responsavelNome: '', responsavelEmail: '', responsavelTelefone: '' },
  })

  // O formulário some ao enviar: o foco vai para o novo título em vez de se perder no documento.
  useEffect(() => {
    if (enviadoPara) titulo.current?.focus()
    else setFocus('razaoSocial')
  }, [enviadoPara, setFocus])

  async function onSubmit(values: SolicitacaoValues) {
    try {
      await enviar.mutateAsync(paraSolicitacao(values))
      setEnviadoPara(values.responsavelEmail)
    } catch (error) {
      if (!applyProblemToForm(error, setError, campos)) setError('root', { message: problemMessage(error) })
    }
  }

  if (enviadoPara) return (
    <AuthShell title="Confirme seu e-mail" headingRef={titulo}>
      <FormNotice tone="success">
        <p>
          Se os dados estiverem corretos, enviamos um link de confirmação para{' '}
          <span className="font-medium break-all text-foreground">{enviadoPara}</span>.
        </p>
        <p className="text-muted-foreground">
          A análise começa depois que você confirmar. No mesmo e-mail vai o link para acompanhar a resposta.
        </p>
      </FormNotice>
      <div className="flex flex-col items-center gap-3">
        <ReenviarLink email={enviadoPara} />
        {/* Os valores continuam no formulário: corrigir não obriga a digitar tudo de novo. */}
        <Button type="button" variant="ghost" size="lg" className={`w-full ${pressable}`} onClick={() => setEnviadoPara(null)}>
          Corrigir dados
        </Button>
        <AuthLink to="/login">Voltar para o login</AuthLink>
      </div>
    </AuthShell>
  )

  const erroId = (campo: Campo) => (errors[campo] ? `solicitacao-${campo}-erro` : undefined)

  return (
    <AuthShell
      title="Solicitar cadastro da empresa"
      description="A equipe Fluxus analisa cada pedido e responde por e-mail. Nenhum acesso é criado antes da aprovação."
    >
      <form noValidate onSubmit={handleSubmit(onSubmit)} className="space-y-8" aria-busy={enviar.isPending}>
        <FieldSet className="gap-5">
          <Legenda>Empresa</Legenda>
          <Campo id="razaoSocial" label="Razão social" erro={errors.razaoSocial?.message} erroId={erroId('razaoSocial')}
            input={{ autoComplete: 'organization', ...register('razaoSocial') }} />
          <Campo id="nomeFantasia" label="Nome fantasia" opcional erro={errors.nomeFantasia?.message} erroId={erroId('nomeFantasia')}
            input={{ autoComplete: 'off', ...register('nomeFantasia') }} />
          <Controller control={control} name="cnpj" render={({ field }) => (
            <Campo id="cnpj" label="CNPJ" erro={errors.cnpj?.message} erroId={erroId('cnpj')}
              ajuda="Aceita CNPJ numérico e alfanumérico."
              input={{
                ref: field.ref, name: field.name, onBlur: field.onBlur, value: formatCnpj(field.value),
                onChange: (event) => field.onChange(normalizeCnpj(event.target.value)),
                placeholder: '00.000.000/0000-00', autoComplete: 'off', autoCapitalize: 'characters', spellCheck: false,
                className: 'tabular-nums',
              }} />
          )} />
        </FieldSet>

        <FieldSet className="gap-5">
          <Legenda>Responsável</Legenda>
          <Campo id="responsavelNome" label="Seu nome" erro={errors.responsavelNome?.message} erroId={erroId('responsavelNome')}
            input={{ autoComplete: 'name', ...register('responsavelNome') }} />
          <Campo id="responsavelEmail" label="E-mail" erro={errors.responsavelEmail?.message} erroId={erroId('responsavelEmail')}
            ajuda="Enviamos o link de confirmação para este e-mail."
            input={{
              type: 'email', autoComplete: 'email', autoCapitalize: 'none', spellCheck: false,
              placeholder: 'nome@empresa.com.br', ...register('responsavelEmail'),
            }} />
          <Controller control={control} name="responsavelTelefone" render={({ field }) => (
            <Campo id="responsavelTelefone" label="Telefone" opcional erro={errors.responsavelTelefone?.message}
              erroId={erroId('responsavelTelefone')}
              input={{
                ref: field.ref, name: field.name, onBlur: field.onBlur, value: formatTelefone(field.value),
                onChange: (event) => field.onChange(normalizeTelefone(event.target.value)),
                type: 'tel', inputMode: 'tel', autoComplete: 'tel-national', placeholder: '(11) 98765-4321',
                className: 'tabular-nums',
              }} />
          )} />
        </FieldSet>

        {errors.root && <FormNotice tone="error">{errors.root.message}</FormNotice>}
        <SubmitButton pending={enviar.isPending} size="lg" className={`w-full ${pressable}`}>Enviar solicitação</SubmitButton>
      </form>
      <p className="self-center text-sm text-muted-foreground">
        Já tem acesso? <AuthLink to="/login" className="text-foreground">Entrar</AuthLink>
      </p>
    </AuthShell>
  )
}

/** Título de cada grupo: pequeno e discreto, separa sem desenhar caixas. */
function Legenda({ children }: { children: ReactNode }) {
  return <FieldLegend className="mb-0 text-xs! font-medium tracking-wide text-muted-foreground uppercase">{children}</FieldLegend>
}

type CampoProps = {
  id: Campo
  label: string
  opcional?: boolean
  ajuda?: string
  erro?: string
  erroId?: string
  input: ComponentProps<typeof Input>
}

function Campo({ id, label, opcional, ajuda, erro, erroId, input }: CampoProps) {
  const inputId = `solicitacao-${id}`
  const ajudaId = ajuda ? `${inputId}-ajuda` : undefined
  return (
    <Field data-invalid={!!erro}>
      <FieldLabel htmlFor={inputId}>
        {label}
        {opcional ? <span className="font-normal text-muted-foreground">(opcional)</span> : null}
      </FieldLabel>
      <Input id={inputId} aria-invalid={!!erro} aria-describedby={erroId ?? ajudaId} {...input} />
      {erro ? <FieldError id={erroId}>{erro}</FieldError> : ajuda ? <FieldDescription id={ajudaId}>{ajuda}</FieldDescription> : null}
    </Field>
  )
}
