import { zodResolver } from '@hookform/resolvers/zod'
import { useForm, useWatch } from 'react-hook-form'
import { toast } from 'sonner'
import { applyProblemToForm, problemMessage } from '@/core/api/problem'
import { SubmitButton } from '@/shared/components/common/SubmitButton'
import { Button } from '@/shared/components/ui/button'
import { Field, FieldDescription, FieldError, FieldGroup, FieldLabel } from '@/shared/components/ui/field'
import { Input } from '@/shared/components/ui/input'
import { SheetClose, SheetFooter } from '@/shared/components/ui/sheet'
import { useCriarPerfil, usePermissoes, useAtualizarPerfil } from '../hooks/use-perfis'
import { perfilFormSchema, type PerfilFormValues } from '../schemas/perfil'
import type { Perfil } from '../types/perfil'

const fields = ['nome', 'descricao', 'permissoes'] as const
const grupos: Record<string, string> = {
  empresas: 'Empresas', filiais: 'Filiais', usuarios: 'Usuários', 'usuarios-empresas': 'Vínculos de usuários', perfis: 'Perfis',
}

export function PerfilForm({ perfil, onSaved }: { perfil?: Perfil; onSaved: () => void }) {
  const editing = perfil !== undefined
  const criar = useCriarPerfil()
  const atualizar = useAtualizarPerfil(perfil?.id ?? '')
  const mutation = editing ? atualizar : criar
  const catalogo = usePermissoes()
  const { control, register, handleSubmit, setError, setValue, formState: { errors } } = useForm<PerfilFormValues>({
    resolver: zodResolver(perfilFormSchema),
    defaultValues: { nome: perfil?.nome ?? '', descricao: perfil?.descricao ?? '', permissoes: perfil?.permissoes ?? [] },
  })
  const selecionadas = useWatch({ control, name: 'permissoes' })

  async function onSubmit(values: PerfilFormValues) {
    try {
      await mutation.mutateAsync({ ...values, permissoes: [...new Set(values.permissoes)] })
      toast.success(editing ? 'Alterações salvas' : 'Perfil cadastrado')
      onSaved()
    } catch (error) {
      if (!applyProblemToForm(error, setError, fields, 'nome')) toast.error(problemMessage(error))
    }
  }

  const errorId = (field: (typeof fields)[number]) => errors[field] ? `perfil-${field}-erro` : undefined
  const gruposCatalogo = (catalogo.data ?? []).reduce<Record<string, typeof catalogo.data>>((all, permissao) => {
    const [grupo] = permissao.codigo.split('.')
    all[grupos[grupo] ?? grupo] ??= []
    all[grupos[grupo] ?? grupo]!.push(permissao)
    return all
  }, {})

  return <form noValidate onSubmit={handleSubmit(onSubmit)} className="flex min-h-0 flex-1 flex-col">
    <FieldGroup className="flex-1 overflow-y-auto overscroll-contain px-4 py-2">
      <Field data-invalid={!!errors.nome}>
        <FieldLabel htmlFor="perfil-nome">Nome</FieldLabel>
        <Input id="perfil-nome" autoComplete="off" aria-invalid={!!errors.nome} aria-describedby={errorId('nome')} {...register('nome')} />
        <FieldError id={errorId('nome')} errors={[errors.nome]} />
      </Field>
      <Field data-invalid={!!errors.descricao}>
        <FieldLabel htmlFor="perfil-descricao">Descrição (opcional)</FieldLabel>
        <Input id="perfil-descricao" autoComplete="off" aria-invalid={!!errors.descricao} aria-describedby={errorId('descricao')} {...register('descricao')} />
        <FieldError id={errorId('descricao')} errors={[errors.descricao]} />
      </Field>
      <fieldset aria-describedby={errors.permissoes ? 'perfil-permissoes-erro' : 'perfil-permissoes-ajuda'} className="space-y-3">
        <legend className="text-sm font-medium">Permissões</legend>
        <FieldDescription id="perfil-permissoes-ajuda">Escolha o que as pessoas com este perfil podem consultar e alterar.</FieldDescription>
        {catalogo.isPending ? <p aria-busy="true" className="text-sm text-muted-foreground">Carregando permissões…</p> : null}
        {catalogo.isError ? <div className="space-y-2"><p className="text-sm text-destructive">{problemMessage(catalogo.error)}</p><Button type="button" variant="outline" onClick={() => catalogo.refetch()}>Tentar de novo</Button></div> : null}
        {Object.entries(gruposCatalogo).map(([grupo, permissoes]) => <fieldset key={grupo} className="space-y-2 rounded-md border p-3">
          <legend className="px-1 text-sm font-medium">{grupo}</legend>
          {permissoes?.map((permissao) => <label key={permissao.codigo} className="flex cursor-pointer items-start gap-2 text-sm">
            <input type="checkbox" className="mt-0.5 size-4 accent-primary" checked={selecionadas.includes(permissao.codigo)} onChange={(event) => {
              const next = event.target.checked ? [...selecionadas, permissao.codigo] : selecionadas.filter((codigo) => codigo !== permissao.codigo)
              setValue('permissoes', next, { shouldDirty: true, shouldValidate: true })
            }} />
            <span><span className="block">{permissao.nome}</span><span className="block text-xs text-muted-foreground">{permissao.descricao}</span></span>
          </label>)}
        </fieldset>)}
        <FieldError id={errorId('permissoes')} errors={[errors.permissoes]} />
      </fieldset>
    </FieldGroup>
    <SheetFooter className="flex-row justify-end border-t">
      <SheetClose render={<Button type="button" variant="outline" />}>Cancelar</SheetClose>
      <SubmitButton pending={mutation.isPending} disabled={catalogo.isPending || catalogo.isError}>{editing ? 'Salvar alterações' : 'Cadastrar perfil'}</SubmitButton>
    </SheetFooter>
  </form>
}
