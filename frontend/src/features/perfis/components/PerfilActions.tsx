import { Ellipsis } from 'lucide-react'
import { useState } from 'react'
import { useNavigate } from 'react-router'
import { notify } from '@/shared/lib/notify'
import { ConfirmDialog } from '@/shared/components/common/ConfirmDialog'
import { Button } from '@/shared/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/shared/components/ui/dropdown-menu'
import { useAlterarStatusPerfil, useExcluirPerfil } from '../hooks/use-perfis'
import type { Perfil } from '../types/perfil'

/**
 * Menu "⋯" da linha. Inativar não pede confirmação: o aviso oferece desfazer.
 * Excluir fica isolado no fim do menu e pede confirmação, porque não tem volta.
 */
export function PerfilActions({ perfil, editHref }: { perfil: Perfil; editHref: string }) {
  const navigate = useNavigate()
  const status = useAlterarStatusPerfil()
  const excluir = useExcluirPerfil()
  const [confirmando, setConfirmando] = useState(false)

  function alterar(ativo: boolean, desfazer = true) {
    status.mutate(
      { id: perfil.id, ativo },
      {
        onSuccess: () =>
          notify.success(ativo ? `${perfil.nome} ativado` : `${perfil.nome} inativado`, {
            undo: desfazer ? () => alterar(!ativo, false) : undefined,
          }),
        onError: (error) => notify.error(error),
      },
    )
  }

  function remover() {
    excluir.mutate(perfil.id, {
      onSuccess: () => {
        setConfirmando(false)
        notify.success(`${perfil.nome} excluído`)
      },
      onError: (error) => notify.error(error),
    })
  }

  return (
    <>
      <DropdownMenu>
        <DropdownMenuTrigger
          render={<Button variant="ghost" size="icon-sm" aria-label={`Ações de ${perfil.nome}`} className="text-muted-foreground" />}
        >
          <Ellipsis />
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="w-48">
          <DropdownMenuItem onClick={() => navigate(editHref)}>Editar perfil</DropdownMenuItem>
          <DropdownMenuSeparator />
          {perfil.ativo ? (
            <DropdownMenuItem onClick={() => alterar(false)}>Inativar perfil</DropdownMenuItem>
          ) : (
            <DropdownMenuItem onClick={() => alterar(true)}>Ativar perfil</DropdownMenuItem>
          )}
          <DropdownMenuSeparator />
          <DropdownMenuItem variant="destructive" onClick={() => setConfirmando(true)}>
            Excluir perfil
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
      <ConfirmDialog
        open={confirmando}
        onOpenChange={setConfirmando}
        title={`Excluir o perfil “${perfil.nome}”?`}
        description="A exclusão não pode ser desfeita. Para só suspender o acesso, inative o perfil."
        confirmLabel="Excluir perfil"
        cancelLabel="Manter perfil"
        pending={excluir.isPending}
        onConfirm={remover}
      />
    </>
  )
}
