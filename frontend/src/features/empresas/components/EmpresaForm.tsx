import { zodResolver } from '@hookform/resolvers/zod'
import { Controller, useForm } from 'react-hook-form'
import { toast } from 'sonner'
import { applyProblemToForm, problemMessage } from '@/core/api/problem'
import { SubmitButton } from '@/shared/components/common/SubmitButton'
import { Button } from '@/shared/components/ui/button'
import { Field, FieldDescription, FieldError, FieldGroup, FieldLabel } from '@/shared/components/ui/field'
import { Input } from '@/shared/components/ui/input'
import { SheetClose, SheetFooter } from '@/shared/components/ui/sheet'
import { useAtualizarEmpresa, useCriarEmpresa } from '../hooks/use-empresas'
import { formatCnpj, normalizeCnpj } from '@/shared/lib/cnpj'
import { empresaFormSchema, type EmpresaFormValues } from '../schemas/empresa'
import type { Empresa } from '../types/empresa'

const fields = ['razaoSocial', 'nomeFantasia', 'cnpj'] as const

type EmpresaFormProps = {
  /** Sem empresa: cadastro. Com empresa: edição (CNPJ somente leitura). */
  empresa?: Empresa
  onSaved: () => void
}

export function EmpresaForm({ empresa, onSaved }: EmpresaFormProps) {
  const editing = empresa !== undefined
  const criar = useCriarEmpresa()
  const atualizar = useAtualizarEmpresa(empresa?.id ?? '')
  const mutation = editing ? atualizar : criar

  const {
    register,
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<EmpresaFormValues>({
    resolver: zodResolver(empresaFormSchema),
    defaultValues: {
      razaoSocial: empresa?.razaoSocial ?? '',
      nomeFantasia: empresa?.nomeFantasia ?? '',
      cnpj: empresa?.cnpj ?? '',
    },
  })

  async function onSubmit(values: EmpresaFormValues) {
    const nomeFantasia = values.nomeFantasia || null

    try {
      if (editing) {
        await atualizar.mutateAsync({ razaoSocial: values.razaoSocial, nomeFantasia })
        toast.success('Alterações salvas')
      } else {
        await criar.mutateAsync({ razaoSocial: values.razaoSocial, nomeFantasia, cnpj: values.cnpj })
        toast.success('Empresa cadastrada')
      }
      onSaved()
    } catch (error) {
      if (!applyProblemToForm(error, setError, fields, 'cnpj')) toast.error(problemMessage(error))
    }
  }

  const errorId = (field: (typeof fields)[number]) => (errors[field] ? `empresa-${field}-erro` : undefined)

  return (
    <form noValidate onSubmit={handleSubmit(onSubmit)} className="flex min-h-0 flex-1 flex-col">
      <FieldGroup className="flex-1 overflow-y-auto overscroll-contain px-4 py-2">
        <Field data-invalid={!!errors.razaoSocial}>
          <FieldLabel htmlFor="empresa-razaoSocial">Razão social</FieldLabel>
          <Input
            id="empresa-razaoSocial"
            autoComplete="organization"
            aria-invalid={!!errors.razaoSocial}
            aria-describedby={errorId('razaoSocial')}
            {...register('razaoSocial')}
          />
          <FieldError id={errorId('razaoSocial')} errors={[errors.razaoSocial]} />
        </Field>

        <Field data-invalid={!!errors.nomeFantasia}>
          <FieldLabel htmlFor="empresa-nomeFantasia">
            Nome fantasia <span className="font-normal text-muted-foreground">(opcional)</span>
          </FieldLabel>
          <Input
            id="empresa-nomeFantasia"
            autoComplete="off"
            aria-invalid={!!errors.nomeFantasia}
            aria-describedby={errorId('nomeFantasia')}
            {...register('nomeFantasia')}
          />
          <FieldError id={errorId('nomeFantasia')} errors={[errors.nomeFantasia]} />
        </Field>

        {editing ? (
          <Field>
            <FieldLabel htmlFor="empresa-cnpj">CNPJ</FieldLabel>
            <Input id="empresa-cnpj" value={formatCnpj(empresa.cnpj)} readOnly
              aria-describedby="empresa-cnpj-ajuda"
              className="border-transparent bg-muted/60 text-muted-foreground tabular-nums focus-visible:bg-transparent"
            />
            <FieldDescription id="empresa-cnpj-ajuda">
              Você não pode alterar o CNPJ. Para usar outro, cadastre uma nova empresa.
            </FieldDescription>
          </Field>
        ) : (
          <Controller
            control={control}
            name="cnpj"
            render={({ field }) => (
              <Field data-invalid={!!errors.cnpj}>
                <FieldLabel htmlFor="empresa-cnpj">CNPJ</FieldLabel>
                <Input
                  id="empresa-cnpj"
                  ref={field.ref}
                  name={field.name}
                  value={formatCnpj(field.value)}
                  onChange={(event) => field.onChange(normalizeCnpj(event.target.value))}
                  onBlur={field.onBlur}
                  placeholder="00.000.000/0000-00"
                  autoComplete="off"
                  autoCapitalize="characters"
                  spellCheck={false}
                  className="tabular-nums"
                  aria-invalid={!!errors.cnpj}
                  aria-describedby={errors.cnpj ? 'empresa-cnpj-erro' : 'empresa-cnpj-ajuda'}
                />
                {errors.cnpj ? (
                  <FieldError id="empresa-cnpj-erro" errors={[errors.cnpj]} />
                ) : (
                  <FieldDescription id="empresa-cnpj-ajuda">Aceita CNPJ numérico e alfanumérico.</FieldDescription>
                )}
              </Field>
            )}
          />
        )}
      </FieldGroup>

      <SheetFooter className="flex-row justify-end border-t">
        <SheetClose render={<Button type="button" variant="outline" />}>Cancelar</SheetClose>
        <SubmitButton pending={mutation.isPending}>{editing ? 'Salvar alterações' : 'Cadastrar empresa'}</SubmitButton>
      </SheetFooter>
    </form>
  )
}
