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
          toast.success(ativo ? `${usuario.nome} ativado` : `${usuario.nome} inativado`, {
            description: ativo ? undefined : 'Não consegue mais entrar no sistema.',
            action: desfazer ? { label: 'Desfazer', onClick: () => alterar(!ativo, false) } : undefined,
          }),
        onError: (error) => toast.error(problemMessage(error)),
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
          <DropdownMenuItem variant="destructive" onClick={() => alterar(false)}>
            Inativar usuário
          </DropdownMenuItem>
        ) : (
          <DropdownMenuItem onClick={() => alterar(true)}>Ativar usuário</DropdownMenuItem>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
