import { isNotFound, problemMessage } from '@/core/api/problem'
import { SheetFormSkeleton, SheetLoadError } from '@/shared/components/common/SheetStates'
import { Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle } from '@/shared/components/ui/sheet'
import { useUsuario } from '../hooks/use-usuarios'
import { UsuarioForm } from './UsuarioForm'

type UsuarioSheetProps = {
  creating: boolean
  editingId: string | null
  onClose: () => void
}

/** Painel lateral de cadastro/edição. Aberto e fechado pela URL (?novo / ?editar=<id>). */
export function UsuarioSheet({ creating, editingId, onClose }: UsuarioSheetProps) {
  const open = creating || editingId !== null

  return (
    <Sheet open={open} onOpenChange={(next) => !next && onClose()}>
      <SheetContent className="sm:max-w-md">
        <SheetHeader className="border-b pr-12">
          <SheetTitle>{editingId ? 'Editar usuário' : 'Novo usuário'}</SheetTitle>
          <SheetDescription>
            {editingId ? 'Nome e e-mail de acesso.' : 'O usuário já começa ativo e entra com este e-mail e senha.'}
          </SheetDescription>
        </SheetHeader>

        {editingId ? <EditarUsuario id={editingId} onSaved={onClose} /> : <UsuarioForm onSaved={onClose} />}
      </SheetContent>
    </Sheet>
  )
}

function EditarUsuario({ id, onSaved }: { id: string; onSaved: () => void }) {
  const { data: usuario, error, isPending } = useUsuario(id)

  if (usuario) return <UsuarioForm key={usuario.id} usuario={usuario} onSaved={onSaved} />
  if (isPending) return <SheetFormSkeleton label="Carregando usuário…" fields={2} />

  return (
    <SheetLoadError
      message={isNotFound(error) ? 'Este usuário não existe mais. Alguém pode tê-lo excluído.' : problemMessage(error)}
    />
  )
}
