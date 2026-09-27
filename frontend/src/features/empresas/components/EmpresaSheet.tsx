import { isNotFound, problemMessage } from '@/core/api/problem'
import { Button } from '@/shared/components/ui/button'
import { Sheet, SheetClose, SheetContent, SheetDescription, SheetHeader, SheetTitle } from '@/shared/components/ui/sheet'
import { Skeleton } from '@/shared/components/ui/skeleton'
import { useEmpresa } from '../hooks/use-empresas'
import { EmpresaForm } from './EmpresaForm'

type EmpresaSheetProps = {
  creating: boolean
  editingId: string | null
  onClose: () => void
}

/** Painel lateral de cadastro/edição. Aberto e fechado pela URL (?novo / ?editar=<id>). */
export function EmpresaSheet({ creating, editingId, onClose }: EmpresaSheetProps) {
  const open = creating || editingId !== null

  return (
    <Sheet open={open} onOpenChange={(next) => !next && onClose()}>
      <SheetContent className="sm:max-w-md">
        <SheetHeader className="border-b pr-12">
          <SheetTitle>{editingId ? 'Editar empresa' : 'Nova empresa'}</SheetTitle>
          <SheetDescription>
            {editingId ? 'Razão social e nome fantasia.' : 'A empresa já começa ativa.'}
          </SheetDescription>
        </SheetHeader>

        {editingId ? <EditarEmpresa id={editingId} onSaved={onClose} /> : <EmpresaForm onSaved={onClose} />}
      </SheetContent>
    </Sheet>
  )
}

function EditarEmpresa({ id, onSaved }: { id: string; onSaved: () => void }) {
  const { data: empresa, error, isPending } = useEmpresa(id)

  if (empresa) return <EmpresaForm key={empresa.id} empresa={empresa} onSaved={onSaved} />

  if (isPending) {
    return (
      <div aria-busy="true" aria-label="Carregando empresa…" className="space-y-6 px-4 py-2">
        {[0, 1, 2].map((item) => (
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
        {isNotFound(error) ? 'Esta empresa não existe mais. Ela pode ter sido excluída.' : problemMessage(error)}
      </p>
      <SheetClose render={<Button variant="outline" />}>Voltar para a lista</SheetClose>
    </div>
  )
}
