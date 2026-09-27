import { isNotFound, problemMessage } from '@/core/api/problem'
import { Button } from '@/shared/components/ui/button'
import { Sheet, SheetClose, SheetContent, SheetDescription, SheetHeader, SheetTitle } from '@/shared/components/ui/sheet'
import { Skeleton } from '@/shared/components/ui/skeleton'
import { useFilial } from '../hooks/use-filiais'
import { FilialForm } from './FilialForm'

export function FilialSheet({ creating, editingId, onClose }: { creating: boolean; editingId: string | null; onClose: () => void }) {
  const open = creating || editingId !== null
  return <Sheet open={open} onOpenChange={(next) => !next && onClose()}><SheetContent className="sm:max-w-md">
    <SheetHeader className="border-b pr-12"><SheetTitle>{editingId ? 'Editar filial' : 'Nova filial'}</SheetTitle><SheetDescription>{editingId ? 'Nome e endereço da unidade.' : 'A filial já começa ativa. Informe o CNPJ e o endereço da unidade.'}</SheetDescription></SheetHeader>
    {editingId ? <EditarFilial id={editingId} onSaved={onClose} /> : <FilialForm onSaved={onClose} />}
  </SheetContent></Sheet>
}
function EditarFilial({ id, onSaved }: { id: string; onSaved: () => void }) {
  const { data, error, isPending } = useFilial(id)
  if (data) return <FilialForm key={data.id} filial={data} onSaved={onSaved} />
  if (isPending) return <div aria-busy="true" aria-label="Carregando filial…" className="space-y-6 px-4 py-2">{[0, 1, 2].map((i) => <div key={i} className="space-y-2"><Skeleton className="h-4 w-24" /><Skeleton className="h-8 w-full" /></div>)}</div>
  return <div className="space-y-4 px-4 py-2"><p className="text-sm text-pretty text-muted-foreground">{isNotFound(error) ? 'Esta filial não existe mais. Ela pode ter sido excluída.' : problemMessage(error)}</p><SheetClose render={<Button variant="outline" />}>Voltar para a lista</SheetClose></div>
}
