import { isNotFound, problemMessage } from '@/core/api/problem'
import { SheetFormSkeleton, SheetLoadError } from '@/shared/components/common/SheetStates'
import { Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle } from '@/shared/components/ui/sheet'
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
  if (isPending) return <SheetFormSkeleton label="Carregando empresa…" />

  return (
    <SheetLoadError
      message={isNotFound(error) ? 'Esta empresa não existe mais. Alguém pode tê-la excluído.' : problemMessage(error)}
    />
  )
}
