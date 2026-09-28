import { Ellipsis } from 'lucide-react'
import { useState } from 'react'
import { toast } from 'sonner'
import { problemMessage } from '@/core/api/problem'
import { ConfirmDialog } from '@/shared/components/common/ConfirmDialog'
import { Button } from '@/shared/components/ui/button'
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from '@/shared/components/ui/dropdown-menu'
import { useCancelarConvite, useReenviarConvite } from '../hooks/use-convites'
import type { Convite } from '../types/convite'

/** Reenviar gera um link novo (o anterior deixa de valer) e renova o prazo; cancelar não tem volta. */
export function ConviteActions({ convite }: { convite: Convite }) {
  const reenviar = useReenviarConvite()
  const cancelar = useCancelarConvite()
  const [confirmando, setConfirmando] = useState(false)
  if (convite.status === 'Aceito' || convite.status === 'Cancelado') return null

  function reenviarConvite() {
    reenviar.mutate(convite.id, {
      onSuccess: () => toast.success(`Convite reenviado para ${convite.email}`, { description: 'O link anterior deixou de valer.' }),
      onError: (error) => toast.error(problemMessage(error)),
    })
  }

  function cancelarConvite() {
    cancelar.mutate(convite.id, {
      onSuccess: () => {
        setConfirmando(false)
        toast.success('Convite cancelado')
      },
      onError: (error) => toast.error(problemMessage(error)),
    })
  }

  return <>
    <DropdownMenu>
      <DropdownMenuTrigger render={<Button variant="ghost" size="icon-sm" aria-label={`Ações do convite de ${convite.email}`} className="text-muted-foreground" />}><Ellipsis /></DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-48">
        <DropdownMenuItem onClick={reenviarConvite} disabled={reenviar.isPending}>Reenviar convite</DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem variant="destructive" onClick={() => setConfirmando(true)}>Cancelar convite</DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
    <ConfirmDialog
      open={confirmando}
      onOpenChange={setConfirmando}
      title={`Cancelar o convite de ${convite.email}?`}
      description="O link enviado deixa de valer. Para convidar de novo, será preciso criar outro convite."
      confirmLabel="Cancelar convite"
      cancelLabel="Manter convite"
      pending={cancelar.isPending}
      onConfirm={cancelarConvite}
    />
  </>
}
