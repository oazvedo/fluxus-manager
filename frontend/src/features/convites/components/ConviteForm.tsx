import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router'
import { toast } from 'sonner'
import { applyProblemToForm, problemMessage } from '@/core/api/problem'
import { SubmitButton } from '@/shared/components/common/SubmitButton'
import { Button } from '@/shared/components/ui/button'
import { Field, FieldDescription, FieldError, FieldGroup, FieldLabel } from '@/shared/components/ui/field'
import { Input } from '@/shared/components/ui/input'
import { SheetClose, SheetFooter } from '@/shared/components/ui/sheet'
import { useCriarConvite, usePerfisAtivos } from '../hooks/use-convites'
import { conviteFormSchema, type ConviteFormValues } from '../schemas/convite'

const fields = ['email', 'perfilId'] as const

// Mesmo visual do Input: não há select no shadcn do projeto, e o nativo já é acessível pelo teclado.
const selectClassName = 'h-8 w-full min-w-0 rounded-lg border border-input bg-background px-2 py-1 text-base outline-none transition-colors focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:cursor-not-allowed disabled:opacity-50 aria-invalid:border-destructive aria-invalid:ring-3 aria-invalid:ring-destructive/20 md:text-sm dark:bg-input/30'

export function ConviteForm({ onSaved }: { onSaved: () => void }) {
  const criar = useCriarConvite()
  const perfis = usePerfisAtivos()
  const { register, handleSubmit, setError, formState: { errors } } = useForm<ConviteFormValues>({
    resolver: zodResolver(conviteFormSchema),
    defaultValues: { email: '', perfilId: '' },
  })

  async function onSubmit(values: ConviteFormValues) {
    try {
      const convite = await criar.mutateAsync(values)
      toast.success(`Convite enviado para ${convite.email}`)
      onSaved()
    } catch (error) {
      if (!applyProblemToForm(error, setError, fields, 'email')) toast.error(problemMessage(error))
    }
  }

  const errorId = (field: (typeof fields)[number]) => errors[field] ? `convite-${field}-erro` : undefined
  const semPerfis = perfis.isSuccess && perfis.data.length === 0

  return <form noValidate onSubmit={handleSubmit(onSubmit)} className="flex min-h-0 flex-1 flex-col">
    <FieldGroup className="flex-1 overflow-y-auto overscroll-contain px-4 py-2">
      <Field data-invalid={!!errors.email}>
        <FieldLabel htmlFor="convite-email">E-mail</FieldLabel>
        <Input id="convite-email" type="email" inputMode="email" autoComplete="off" autoCapitalize="none" spellCheck={false}
          placeholder="nome@empresa.com.br" aria-invalid={!!errors.email} aria-describedby={errorId('email')} {...register('email')} />
        <FieldError id={errorId('email')} errors={[errors.email]} />
      </Field>
      <Field data-invalid={!!errors.perfilId}>
        <FieldLabel htmlFor="convite-perfilId">Perfil de acesso</FieldLabel>
        <select id="convite-perfilId" className={selectClassName} disabled={!perfis.isSuccess || semPerfis}
          aria-invalid={!!errors.perfilId} aria-describedby={errors.perfilId ? 'convite-perfilId-erro' : perfis.isError ? 'convite-perfis-falha' : 'convite-perfilId-ajuda'}
          aria-busy={perfis.isPending || undefined} {...register('perfilId')}>
          <option value="">{perfis.isPending ? 'Carregando perfis…' : 'Escolha um perfil'}</option>
          {perfis.data?.map((perfil) => <option key={perfil.id} value={perfil.id}>{perfil.nome}</option>)}
        </select>
        {perfis.isError ? <div className="space-y-2">
          <p id="convite-perfis-falha" role="alert" className="text-sm text-destructive">{problemMessage(perfis.error)}</p>
          <Button type="button" variant="outline" onClick={() => perfis.refetch()}>Tentar de novo</Button>
        </div> : <FieldDescription id="convite-perfilId-ajuda">
          {semPerfis
            ? <>Esta empresa não tem perfil ativo. <Link to="/perfis?novo" className="font-medium text-foreground underline underline-offset-4">Cadastre um perfil</Link> antes de convidar.</>
            : 'Define o que a pessoa pode consultar e alterar depois de aceitar.'}
        </FieldDescription>}
        <FieldError id={errorId('perfilId')} errors={[errors.perfilId]} />
      </Field>
    </FieldGroup>
    <SheetFooter className="flex-row justify-end border-t">
      <SheetClose render={<Button type="button" variant="outline" />}>Cancelar</SheetClose>
      <SubmitButton pending={criar.isPending} disabled={!perfis.isSuccess || semPerfis}>Enviar convite</SubmitButton>
    </SheetFooter>
  </form>
}
