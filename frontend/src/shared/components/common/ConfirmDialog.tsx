import { useRef } from 'react'
import { SubmitButton } from '@/shared/components/common/SubmitButton'
import {
  AlertDialog,
  AlertDialogClose,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/shared/components/ui/alert-dialog'
import { Button } from '@/shared/components/ui/button'

type ConfirmDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  /** Pergunta direta: "Excluir o perfil Financeiro?" */
  title: string
  /** A consequência, em uma frase. */
  description: string
  /** Verbo da ação, repetindo a consequência: "Excluir perfil". */
  confirmLabel: string
  /** O que acontece se desistir: "Manter perfil". */
  cancelLabel: string
  pending?: boolean
  onConfirm: () => void
}

/**
 * Confirmação de ação sem volta. O foco abre no botão seguro, Esc cancela
 * e o botão de confirmar fica em vermelho, a única cor para "irreversível".
 */
export function ConfirmDialog({
  open,
  onOpenChange,
  title,
  description,
  confirmLabel,
  cancelLabel,
  pending = false,
  onConfirm,
}: ConfirmDialogProps) {
  const cancelRef = useRef<HTMLButtonElement>(null)

  return (
    <AlertDialog open={open} onOpenChange={(next) => !pending && onOpenChange(next)}>
      <AlertDialogContent initialFocus={cancelRef}>
        <AlertDialogHeader>
          <AlertDialogTitle>{title}</AlertDialogTitle>
          <AlertDialogDescription>{description}</AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogClose ref={cancelRef} disabled={pending} render={<Button variant="outline" />}>
            {cancelLabel}
          </AlertDialogClose>
          <SubmitButton type="button" variant="destructive" pending={pending} onClick={onConfirm}>
            {confirmLabel}
          </SubmitButton>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
