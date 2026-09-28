import { isNotFound, problemMessage } from '@/core/api/problem'
import { SheetFormSkeleton, SheetLoadError } from '@/shared/components/common/SheetStates'
import { Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle } from '@/shared/components/ui/sheet'
import { useFilial } from '../hooks/use-filiais'
import { FilialForm } from './FilialForm'

type FilialSheetProps = {
  creating: boolean
  editingId: string | null
  onClose: () => void
}

/** Painel lateral de cadastro/edição. Aberto e fechado pela URL (?novo / ?editar=<id>). */
export function FilialSheet({ creating, editingId, onClose }: FilialSheetProps) {
  const open = creating || editingId !== null

  return (
    <Sheet open={open} onOpenChange={(next) => !next && onClose()}>
      <SheetContent className="sm:max-w-md">
        <SheetHeader className="border-b pr-12">
          <SheetTitle>{editingId ? 'Editar filial' : 'Nova filial'}</SheetTitle>
          <SheetDescription>{editingId ? 'Nome e endereço da unidade.' : 'A filial já começa ativa.'}</SheetDescription>
        </SheetHeader>

        {editingId ? <EditarFilial id={editingId} onSaved={onClose} /> : <FilialForm onSaved={onClose} />}
      </SheetContent>
    </Sheet>
  )
}

function EditarFilial({ id, onSaved }: { id: string; onSaved: () => void }) {
  const { data: filial, error, isPending } = useFilial(id)

  if (filial) return <FilialForm key={filial.id} filial={filial} onSaved={onSaved} />
  if (isPending) return <SheetFormSkeleton label="Carregando filial…" />

  return (
    <SheetLoadError
      message={isNotFound(error) ? 'Esta filial não existe mais. Alguém pode tê-la excluído.' : problemMessage(error)}
    />
  )
}
