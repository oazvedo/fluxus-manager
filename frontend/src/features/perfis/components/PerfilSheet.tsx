import { isNotFound, problemMessage } from '@/core/api/problem'
import { SheetFormSkeleton, SheetLoadError } from '@/shared/components/common/SheetStates'
import { Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle } from '@/shared/components/ui/sheet'
import { usePerfil } from '../hooks/use-perfis'
import { PerfilForm } from './PerfilForm'

type PerfilSheetProps = {
  creating: boolean
  editingId: string | null
  onClose: () => void
}

/** Painel lateral de cadastro/edição. Aberto e fechado pela URL (?novo / ?editar=<id>). */
export function PerfilSheet({ creating, editingId, onClose }: PerfilSheetProps) {
  const open = creating || editingId !== null

  return (
    <Sheet open={open} onOpenChange={(next) => !next && onClose()}>
      <SheetContent className="sm:max-w-lg">
        <SheetHeader className="border-b pr-12">
          <SheetTitle>{editingId ? 'Editar perfil' : 'Novo perfil'}</SheetTitle>
          <SheetDescription>
            {editingId ? 'Nome, descrição e permissões.' : 'Defina o que as pessoas com este perfil podem fazer.'}
          </SheetDescription>
        </SheetHeader>

        {editingId ? <EditarPerfil id={editingId} onSaved={onClose} /> : <PerfilForm onSaved={onClose} />}
      </SheetContent>
    </Sheet>
  )
}

function EditarPerfil({ id, onSaved }: { id: string; onSaved: () => void }) {
  const { data: perfil, error, isPending } = usePerfil(id)

  if (perfil) return <PerfilForm key={perfil.id} perfil={perfil} onSaved={onSaved} />
  if (isPending) return <SheetFormSkeleton label="Carregando perfil…" />

  return (
    <SheetLoadError
      message={isNotFound(error) ? 'Este perfil não existe mais ou pertence a outra empresa.' : problemMessage(error)}
    />
  )
}
