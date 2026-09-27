import { Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle } from '@/shared/components/ui/sheet'
import { ConviteForm } from './ConviteForm'

export function ConviteSheet({ open, onClose }: { open: boolean; onClose: () => void }) {
  return <Sheet open={open} onOpenChange={(next) => !next && onClose()}>
    <SheetContent className="sm:max-w-md">
      <SheetHeader className="border-b pr-12">
        <SheetTitle>Novo convite</SheetTitle>
        <SheetDescription>A pessoa recebe um link por e-mail para acessar esta empresa. O link vale por alguns dias.</SheetDescription>
      </SheetHeader>
      {open ? <ConviteForm onSaved={onClose} /> : null}
    </SheetContent>
  </Sheet>
}
