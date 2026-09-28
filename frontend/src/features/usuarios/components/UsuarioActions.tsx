import { Ellipsis } from 'lucide-react'
import { useNavigate } from 'react-router'
import { notify } from '@/shared/lib/notify'
import { Button } from '@/shared/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/shared/components/ui/dropdown-menu'
import { useAlterarStatusUsuario } from '../hooks/use-usuarios'
import type { Usuario } from '../types/usuario'

/** Menu "⋯" da linha: editar e ativar/inativar. Inativar não pede confirmação: o aviso oferece desfazer. */
export function UsuarioActions({ usuario, editHref }: { usuario: Usuario; editHref: string }) {
  const navigate = useNavigate()
  const alterarStatus = useAlterarStatusUsuario()

  function alterar(ativo: boolean, desfazer = true) {
    alterarStatus.mutate(
      { id: usuario.id, ativo },
      {
        onSuccess: () =>
          notify.success(ativo ? `${usuario.nome} ativado` : `${usuario.nome} inativado`, {
            description: ativo ? undefined : 'Esta pessoa não consegue mais entrar no sistema.',
            undo: desfazer ? () => alterar(!ativo, false) : undefined,
          }),
        onError: (error) => notify.error(error),
      },
    )
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger
        render={<Button variant="ghost" size="icon-sm" aria-label={`Ações de ${usuario.nome}`} className="text-muted-foreground" />}
      >
        <Ellipsis />
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-44">
        <DropdownMenuItem onClick={() => navigate(editHref)}>Editar usuário</DropdownMenuItem>
        <DropdownMenuSeparator />
        {usuario.ativo ? (
          <DropdownMenuItem onClick={() => alterar(false)}>
            Inativar usuário
          </DropdownMenuItem>
        ) : (
          <DropdownMenuItem onClick={() => alterar(true)}>Ativar usuário</DropdownMenuItem>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
