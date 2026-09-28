import { zodResolver } from '@hookform/resolvers/zod'
import { useState, type ReactNode } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useLocation } from 'react-router'
import { applyProblemToForm, getProblem, problemMessage } from '@/core/api/problem'
import { SubmitButton } from '@/shared/components/common/SubmitButton'
import { Button, buttonVariants } from '@/shared/components/ui/button'
import { Field, FieldError, FieldLabel } from '@/shared/components/ui/field'
import { Input } from '@/shared/components/ui/input'
import { Skeleton } from '@/shared/components/ui/skeleton'
import { formatDateTime } from '@/shared/lib/format'
import { cn } from '@/shared/lib/utils'
import { ThemeToggle } from '@/shared/components/common/ThemeToggle'
import { useAceitarConvite, useConviteDetalhes } from '../hooks/use-convites'
import { tokenDoLink } from '../lib/token'
import { aceiteNovoUsuarioSchema, type AceiteNovoUsuarioValues } from '../schemas/convite'
import type { ConviteDetalhes } from '../types/convite'

/** Tela pública do link do e-mail: mostra o convite e cria o acesso (ou só vincula quem já tem conta). */
export function AceitarConvitePage() {
  const token = tokenDoLink(useLocation().search)
  const detalhes = useConviteDetalhes(token)
  const [aceitoPor, setAceitoPor] = useState<string | null>(null)

  let conteudo: ReactNode
  if (!token) conteudo = <Aviso titulo="Link de convite inválido"
    texto="Abra de novo o link do e-mail. Se você copiou o link, confira se ele veio inteiro." />
  else if (aceitoPor) conteudo = <Aviso titulo="Convite aceito" texto={`Entre com ${aceitoPor} e a sua senha para acessar a empresa.`}
    acao={<Link to="/login" state={{ email: aceitoPor }} className={cn(buttonVariants())}>Entrar</Link>} />
  else if (detalhes.isPending) conteudo = <div role="status" aria-label="Carregando convite…" className="space-y-3">
    <Skeleton className="h-6 w-2/3" /><Skeleton className="h-4 w-full" /><Skeleton className="h-4 w-4/5" /><Skeleton className="mt-6 h-8 w-full" />
  </div>
  else if (detalhes.isError) conteudo = <ErroDoConvite error={detalhes.error} tentarDeNovo={() => detalhes.refetch()} />
  else conteudo = <Convite detalhes={detalhes.data} token={token} onAceito={setAceitoPor} />

  return <main className="flex min-h-svh items-center justify-center px-6 py-12">
    <div className="w-full max-w-sm space-y-8">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3 font-semibold tracking-tight">
          <span aria-hidden="true" className="flex size-8 items-center justify-center rounded-lg bg-foreground text-background">F</span>
          FluxusManager
        </div>
        <ThemeToggle />
      </div>
      {conteudo}
    </div>
  </main>
}

function Convite({ detalhes, token, onAceito }: { detalhes: ConviteDetalhes; token: string; onAceito: (email: string) => void }) {
  return <div className="space-y-6">
    <div className="space-y-2">
      <h1 className="text-xl font-semibold tracking-tight text-balance">Convite para {detalhes.empresa}</h1>
      <p className="text-sm text-pretty text-muted-foreground">
        Acesso para <span className="font-medium text-foreground">{detalhes.email}</span> com o perfil {detalhes.perfil}.
        {' '}Válido até <time dateTime={detalhes.expiraEm}>{formatDateTime(detalhes.expiraEm)}</time>.
      </p>
    </div>
    {detalhes.usuarioExistente
      ? <AceiteUsuarioExistente token={token} onAceito={() => onAceito(detalhes.email)} />
      : <AceiteNovoUsuario token={token} onAceito={() => onAceito(detalhes.email)} />}
  </div>
}

function AceiteUsuarioExistente({ token, onAceito }: { token: string; onAceito: () => void }) {
  const aceitar = useAceitarConvite()
  const [erro, setErro] = useState<string | null>(null)

  function aceitarConvite() {
    setErro(null)
    aceitar.mutate({ token, nome: null, senha: null }, { onSuccess: onAceito, onError: (error) => setErro(problemMessage(error)) })
  }

  return <div className="space-y-4">
    <p className="text-sm text-pretty text-muted-foreground">Você já tem conta com este e-mail. Aceite e você passa a ver esta empresa ao entrar, com a mesma senha.</p>
    {erro ? <p role="alert" className="text-sm text-destructive">{erro}</p> : null}
    <Button className="w-full" onClick={aceitarConvite} disabled={aceitar.isPending} aria-busy={aceitar.isPending || undefined}>Aceitar convite</Button>
  </div>
}

const fields = ['nome', 'senha'] as const

function AceiteNovoUsuario({ token, onAceito }: { token: string; onAceito: () => void }) {
  const aceitar = useAceitarConvite()
  const { register, handleSubmit, setError, formState: { errors } } = useForm<AceiteNovoUsuarioValues>({
    resolver: zodResolver(aceiteNovoUsuarioSchema), defaultValues: { nome: '', senha: '', confirmacao: '' },
  })

  async function onSubmit(values: AceiteNovoUsuarioValues) {
    try {
      await aceitar.mutateAsync({ token, nome: values.nome, senha: values.senha })
      onAceito()
    } catch (error) {
      if (!applyProblemToForm(error, setError, fields)) setError('root', { message: problemMessage(error) })
    }
  }

  const errorId = (field: 'nome' | 'senha' | 'confirmacao') => errors[field] ? `aceite-${field}-erro` : undefined
  return <form noValidate onSubmit={handleSubmit(onSubmit)} className="space-y-5" aria-busy={aceitar.isPending}>
    <p className="text-sm text-pretty text-muted-foreground">Informe seu nome e crie uma senha para entrar no sistema.</p>
    <Field data-invalid={!!errors.nome}>
      <FieldLabel htmlFor="aceite-nome">Nome</FieldLabel>
      <Input id="aceite-nome" autoComplete="name" aria-invalid={!!errors.nome} aria-describedby={errorId('nome')} {...register('nome')} />
      <FieldError id={errorId('nome')} errors={[errors.nome]} />
    </Field>
    <Field data-invalid={!!errors.senha}>
      <FieldLabel htmlFor="aceite-senha">Senha</FieldLabel>
      <Input id="aceite-senha" type="password" autoComplete="new-password" aria-invalid={!!errors.senha}
        aria-describedby={errorId('senha') ?? 'aceite-senha-ajuda'} {...register('senha')} />
      <p id="aceite-senha-ajuda" className="text-xs text-muted-foreground">Use pelo menos 8 caracteres, com letras e números.</p>
      <FieldError id={errorId('senha')} errors={[errors.senha]} />
    </Field>
    <Field data-invalid={!!errors.confirmacao}>
      <FieldLabel htmlFor="aceite-confirmacao">Repita a senha</FieldLabel>
      <Input id="aceite-confirmacao" type="password" autoComplete="new-password" aria-invalid={!!errors.confirmacao}
        aria-describedby={errorId('confirmacao')} {...register('confirmacao')} />
      <FieldError id={errorId('confirmacao')} errors={[errors.confirmacao]} />
    </Field>
    {errors.root ? <p role="alert" className="text-sm text-destructive">{errors.root.message}</p> : null}
    <SubmitButton pending={aceitar.isPending} className="w-full">Criar acesso</SubmitButton>
  </form>
}

/** 404 e 422 descrevem o link (não encontrado, expirado, cancelado, já aceito); outros erros podem ser tentados de novo. */
function ErroDoConvite({ error, tentarDeNovo }: { error: unknown; tentarDeNovo: () => void }) {
  const status = getProblem(error)?.status
  if (status === 404 || status === 422 || status === 400) return <Aviso titulo="Este convite não pode ser usado"
    texto={problemMessage(error)} acao={<Link to="/login" className={cn(buttonVariants({ variant: 'outline' }))}>Ir para o login</Link>} />
  return <Aviso titulo="Não foi possível abrir o convite" texto={problemMessage(error)}
    acao={<Button variant="outline" onClick={tentarDeNovo}>Tentar de novo</Button>} />
}

function Aviso({ titulo, texto, acao }: { titulo: string; texto: string; acao?: ReactNode }) {
  return <div className="space-y-4">
    <div className="space-y-2">
      <h1 className="text-xl font-semibold tracking-tight">{titulo}</h1>
      <p className="text-sm text-pretty text-muted-foreground">{texto}</p>
    </div>
    {acao}
  </div>
}
