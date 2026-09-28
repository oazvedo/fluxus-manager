import { Ellipsis } from 'lucide-react'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { problemMessage } from '@/core/api/problem'
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

  function alterar(ativo: boolean, desfazer = true) {
    status.mutate(
      { id: perfil.id, ativo },
      {
        onSuccess: () =>
          toast.success(ativo ? `${perfil.nome} ativado` : `${perfil.nome} inativado`, {
            action: desfazer ? { label: 'Desfazer', onClick: () => alterar(!ativo, false) } : undefined,
          }),
        onError: (error) => toast.error(problemMessage(error)),
      },
    )
  }

  function remover() {
    if (!window.confirm(`Excluir o perfil “${perfil.nome}”? Você não poderá desfazer a exclusão.`)) return
    excluir.mutate(perfil.id, {
      onSuccess: () => toast.success(`${perfil.nome} excluído`),
      onError: (error) => toast.error(problemMessage(error)),
    })
  }

  return (
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
        <DropdownMenuItem variant="destructive" onClick={remover} disabled={excluir.isPending}>
          Excluir perfil
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
