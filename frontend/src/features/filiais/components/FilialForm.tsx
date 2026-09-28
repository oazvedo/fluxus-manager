import { zodResolver } from '@hookform/resolvers/zod'
import { Controller, useForm } from 'react-hook-form'
import { notify } from '@/shared/lib/notify'
import { destacarRegistro } from '@/shared/lib/record-highlight'
import { applyProblemToForm } from '@/core/api/problem'
import { SubmitButton } from '@/shared/components/common/SubmitButton'
import { Button } from '@/shared/components/ui/button'
import { Field, FieldDescription, FieldError, FieldGroup, FieldLabel } from '@/shared/components/ui/field'
import { Input } from '@/shared/components/ui/input'
import { SheetClose, SheetFooter } from '@/shared/components/ui/sheet'
import { useAtualizarFilial, useCriarFilial } from '../hooks/use-filiais'
import { formatCnpj, normalizeCnpj } from '@/shared/lib/cnpj'
import { filialFormSchema, type FilialFormValues } from '../schemas/filial'
import type { Filial } from '../types/filial'

const fields = ['nome', 'cnpj', 'endereco'] as const

type FilialFormProps = {
  /** Sem filial: cadastro. Com filial: edição (CNPJ somente leitura). */
  filial?: Filial
  onSaved: () => void
}

export function FilialForm({ filial, onSaved }: FilialFormProps) {
  const editing = filial !== undefined
  const criar = useCriarFilial()
  const atualizar = useAtualizarFilial(filial?.id ?? '')
  const mutation = editing ? atualizar : criar

  const {
    register,
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<FilialFormValues>({
    resolver: zodResolver(filialFormSchema),
    defaultValues: { nome: filial?.nome ?? '', cnpj: filial?.cnpj ?? '', endereco: filial?.endereco ?? '' },
  })

  async function onSubmit(values: FilialFormValues) {
    try {
      if (editing) {
        const { id } = await atualizar.mutateAsync({ nome: values.nome, endereco: values.endereco })
        destacarRegistro(id)
        notify.success('Alterações salvas')
      } else {
        const { id } = await criar.mutateAsync(values)
        destacarRegistro(id)
        notify.success('Filial cadastrada')
      }
      onSaved()
    } catch (error) {
      if (!applyProblemToForm(error, setError, fields, 'cnpj')) notify.error(error)
    }
  }

  const errorId = (field: (typeof fields)[number]) => (errors[field] ? `filial-${field}-erro` : undefined)

  return (
    <form noValidate onSubmit={handleSubmit(onSubmit)} className="flex min-h-0 flex-1 flex-col">
      <FieldGroup className="flex-1 overflow-y-auto overscroll-contain px-4 py-2">
        <Field data-invalid={!!errors.nome}>
          <FieldLabel htmlFor="filial-nome">Nome da filial</FieldLabel>
          <Input
            id="filial-nome"
            autoComplete="off"
            placeholder="Ex.: Unidade Centro"
            aria-invalid={!!errors.nome}
            aria-describedby={errorId('nome')}
            {...register('nome')}
          />
          <FieldError id={errorId('nome')} errors={[errors.nome]} />
        </Field>

        {editing ? (
          <Field>
            <FieldLabel htmlFor="filial-cnpj">CNPJ</FieldLabel>
            <Input id="filial-cnpj" value={formatCnpj(filial.cnpj)} readOnly
              aria-describedby="filial-cnpj-ajuda"
              className="border-transparent bg-muted/60 text-muted-foreground tabular-nums focus-visible:bg-transparent"
            />
            <FieldDescription id="filial-cnpj-ajuda">
              Você não pode alterar o CNPJ. Para usar outro, cadastre uma nova filial.
            </FieldDescription>
          </Field>
        ) : (
          <Controller
            control={control}
            name="cnpj"
            render={({ field }) => (
              <Field data-invalid={!!errors.cnpj}>
                <FieldLabel htmlFor="filial-cnpj">CNPJ</FieldLabel>
                <Input
                  id="filial-cnpj"
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
                  aria-describedby={errors.cnpj ? 'filial-cnpj-erro' : 'filial-cnpj-ajuda'}
                />
                {errors.cnpj ? (
                  <FieldError id="filial-cnpj-erro" errors={[errors.cnpj]} />
                ) : (
                  <FieldDescription id="filial-cnpj-ajuda">Aceita CNPJ numérico e alfanumérico.</FieldDescription>
                )}
              </Field>
            )}
          />
        )}

        <Field data-invalid={!!errors.endereco}>
          <FieldLabel htmlFor="filial-endereco">Endereço</FieldLabel>
          <Input
            id="filial-endereco"
            autoComplete="street-address"
            placeholder="Rua, número, bairro e cidade"
            aria-invalid={!!errors.endereco}
            aria-describedby={errorId('endereco')}
            {...register('endereco')}
          />
          <FieldError id={errorId('endereco')} errors={[errors.endereco]} />
        </Field>
      </FieldGroup>

      <SheetFooter className="flex-row justify-end border-t">
        <SheetClose render={<Button type="button" variant="outline" />}>Cancelar</SheetClose>
        <SubmitButton pending={mutation.isPending}>{editing ? 'Salvar alterações' : 'Cadastrar filial'}</SubmitButton>
      </SheetFooter>
    </form>
  )
}
