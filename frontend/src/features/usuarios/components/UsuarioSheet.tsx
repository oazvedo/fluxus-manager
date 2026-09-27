import { isNotFound, problemMessage } from '@/core/api/problem'
import { Button } from '@/shared/components/ui/button'
import { Sheet, SheetClose, SheetContent, SheetDescription, SheetHeader, SheetTitle } from '@/shared/components/ui/sheet'
import { Skeleton } from '@/shared/components/ui/skeleton'
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

  if (isPending) {
    return (
      <div aria-busy="true" aria-label="Carregando usuário…" className="space-y-6 px-4 py-2">
        {[0, 1].map((item) => (
          <div key={item} className="space-y-2">
            <Skeleton className="h-4 w-24" />
            <Skeleton className="h-8 w-full" />
          </div>
        ))}
      </div>
    )
  }

  return (
    <div className="space-y-4 px-4 py-2">
      <p className="text-sm text-pretty text-muted-foreground">
        {isNotFound(error) ? 'Este usuário não existe mais. Ele pode ter sido excluído.' : problemMessage(error)}
      </p>
      <SheetClose render={<Button variant="outline" />}>Voltar para a lista</SheetClose>
    </div>
  )
}
