import { isNotFound, problemMessage } from '@/core/api/problem'
import { Button } from '@/shared/components/ui/button'
import { Sheet, SheetClose, SheetContent, SheetDescription, SheetHeader, SheetTitle } from '@/shared/components/ui/sheet'
import { Skeleton } from '@/shared/components/ui/skeleton'
import { usePerfil } from '../hooks/use-perfis'
import { PerfilForm } from './PerfilForm'

export function PerfilSheet({ creating, editingId, onClose }: { creating: boolean; editingId: string | null; onClose: () => void }) {
  const open = creating || editingId !== null
  return <Sheet open={open} onOpenChange={(next) => !next && onClose()}>
    <SheetContent className="sm:max-w-lg">
      <SheetHeader className="border-b pr-12">
        <SheetTitle>{editingId ? 'Editar perfil' : 'Novo perfil'}</SheetTitle>
        <SheetDescription>{editingId ? 'Nome, descrição e permissões deste perfil.' : 'Defina o que as pessoas com este perfil podem fazer.'}</SheetDescription>
      </SheetHeader>
      {editingId ? <EditarPerfil id={editingId} onSaved={onClose} /> : <PerfilForm onSaved={onClose} />}
    </SheetContent>
  </Sheet>
}

function EditarPerfil({ id, onSaved }: { id: string; onSaved: () => void }) {
  const { data: perfil, error, isPending } = usePerfil(id)
  if (perfil) return <PerfilForm key={perfil.id} perfil={perfil} onSaved={onSaved} />
  if (isPending) return <div aria-busy="true" aria-label="Carregando perfil…" className="space-y-6 px-4 py-2">
    {[0, 1, 2].map((item) => <div key={item} className="space-y-2"><Skeleton className="h-4 w-24" /><Skeleton className="h-8 w-full" /></div>)}
  </div>
  return <div className="space-y-4 px-4 py-2">
    <p className="text-sm text-pretty text-muted-foreground">{isNotFound(error) ? 'Este perfil não existe mais ou não pertence a esta empresa.' : problemMessage(error)}</p>
    <SheetClose render={<Button variant="outline" />}>Voltar para a lista</SheetClose>
  </div>
}
