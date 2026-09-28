import { zodResolver } from '@hookform/resolvers/zod'
import { Eye, EyeOff } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { notify } from '@/shared/lib/notify'
import { applyProblemToForm } from '@/core/api/problem'
import { SubmitButton } from '@/shared/components/common/SubmitButton'
import { Button } from '@/shared/components/ui/button'
import { Field, FieldDescription, FieldError, FieldGroup, FieldLabel } from '@/shared/components/ui/field'
import { Input } from '@/shared/components/ui/input'
import { SheetClose, SheetFooter } from '@/shared/components/ui/sheet'
import { useAtualizarUsuario, useCriarUsuario } from '../hooks/use-usuarios'
import { editarUsuarioFormSchema, usuarioFormSchema, type UsuarioFormValues } from '../schemas/usuario'
import type { Usuario } from '../types/usuario'

const fields = ['nome', 'email', 'senha'] as const

type UsuarioFormProps = {
  /** Sem usuário: cadastro, com senha. Com usuário: edição, sem senha. */
  usuario?: Usuario
  onSaved: () => void
}

export function UsuarioForm({ usuario, onSaved }: UsuarioFormProps) {
  const editing = usuario !== undefined
  const criar = useCriarUsuario()
  const atualizar = useAtualizarUsuario(usuario?.id ?? '')
  const mutation = editing ? atualizar : criar
  const [showSenha, setShowSenha] = useState(false)

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<UsuarioFormValues>({
    resolver: zodResolver(editing ? editarUsuarioFormSchema : usuarioFormSchema),
    defaultValues: {
      nome: usuario?.nome ?? '',
      email: usuario?.email ?? '',
      senha: '',
    },
  })

  async function onSubmit(values: UsuarioFormValues) {
    try {
      if (editing) {
        await atualizar.mutateAsync({ nome: values.nome, email: values.email })
        notify.success('Alterações salvas')
      } else {
        await criar.mutateAsync(values)
        notify.success('Usuário cadastrado')
      }
      onSaved()
    } catch (error) {
      if (!applyProblemToForm(error, setError, fields, 'email')) notify.error(error)
    }
  }

  const errorId = (field: (typeof fields)[number]) => (errors[field] ? `usuario-${field}-erro` : undefined)

  return (
    <form noValidate onSubmit={handleSubmit(onSubmit)} className="flex min-h-0 flex-1 flex-col">
      <FieldGroup className="flex-1 overflow-y-auto overscroll-contain px-4 py-2">
        <Field data-invalid={!!errors.nome}>
          <FieldLabel htmlFor="usuario-nome">Nome</FieldLabel>
          <Input
            id="usuario-nome"
            autoComplete="off"
            aria-invalid={!!errors.nome}
            aria-describedby={errorId('nome')}
            {...register('nome')}
          />
          <FieldError id={errorId('nome')} errors={[errors.nome]} />
        </Field>

        <Field data-invalid={!!errors.email}>
          <FieldLabel htmlFor="usuario-email">E-mail</FieldLabel>
          <Input
            id="usuario-email"
            type="email"
            inputMode="email"
            autoComplete="off"
            autoCapitalize="none"
            spellCheck={false}
            placeholder="nome@empresa.com.br"
            aria-invalid={!!errors.email}
            aria-describedby={errorId('email')}
            {...register('email')}
          />
          <FieldError id={errorId('email')} errors={[errors.email]} />
        </Field>

        {editing ? null : (
          <Field data-invalid={!!errors.senha}>
            <FieldLabel htmlFor="usuario-senha">Senha</FieldLabel>
            <div className="relative">
              <Input
                id="usuario-senha"
                type={showSenha ? 'text' : 'password'}
                autoComplete="new-password"
                spellCheck={false}
                className="pr-9"
                aria-invalid={!!errors.senha}
                aria-describedby={errors.senha ? 'usuario-senha-erro' : 'usuario-senha-ajuda'}
                {...register('senha')}
              />
              <Button
                type="button"
                variant="ghost"
                size="icon-sm"
                className="absolute top-1/2 right-0.5 -translate-y-1/2 text-muted-foreground"
                aria-label={showSenha ? 'Ocultar senha' : 'Mostrar senha'}
                aria-pressed={showSenha}
                onClick={() => setShowSenha((value) => !value)}
              >
                {showSenha ? <EyeOff aria-hidden="true" /> : <Eye aria-hidden="true" />}
              </Button>
            </div>
            {errors.senha ? (
              <FieldError id="usuario-senha-erro" errors={[errors.senha]} />
            ) : (
              <FieldDescription id="usuario-senha-ajuda">Use pelo menos 8 caracteres, com letras e números.</FieldDescription>
            )}
          </Field>
        )}
      </FieldGroup>

      <SheetFooter className="flex-row justify-end border-t">
        <SheetClose render={<Button type="button" variant="outline" />}>Cancelar</SheetClose>
        <SubmitButton pending={mutation.isPending}>{editing ? 'Salvar alterações' : 'Cadastrar usuário'}</SubmitButton>
      </SheetFooter>
    </form>
  )
}
