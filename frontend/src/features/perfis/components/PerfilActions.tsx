import { Ellipsis } from 'lucide-react'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { problemMessage } from '@/core/api/problem'
import { Button } from '@/shared/components/ui/button'
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from '@/shared/components/ui/dropdown-menu'
import { useAlterarStatusPerfil, useExcluirPerfil } from '../hooks/use-perfis'
import type { Perfil } from '../types/perfil'

export function PerfilActions({ perfil, editHref }: { perfil: Perfil; editHref: string }) {
  const navigate = useNavigate()
  const status = useAlterarStatusPerfil()
  const excluir = useExcluirPerfil()

  function alterar(ativo: boolean, desfazer = true) {
    status.mutate({ id: perfil.id, ativo }, {
      onSuccess: () => toast.success(ativo ? `${perfil.nome} ativado` : `${perfil.nome} inativado`, {
        action: desfazer ? { label: 'Desfazer', onClick: () => alterar(!ativo, false) } : undefined,
      }),
      onError: (error) => toast.error(problemMessage(error)),
    })
  }

  function remover() {
    if (!window.confirm(`Excluir o perfil “${perfil.nome}”? Esta ação não pode ser desfeita.`)) return
    excluir.mutate(perfil.id, {
      onSuccess: () => toast.success('Perfil excluído'),
      onError: (error) => toast.error(problemMessage(error)),
    })
  }

  return <DropdownMenu>
    <DropdownMenuTrigger render={<Button variant="ghost" size="icon-sm" aria-label={`Ações de ${perfil.nome}`} className="text-muted-foreground" />}><Ellipsis /></DropdownMenuTrigger>
    <DropdownMenuContent align="end" className="w-48">
      <DropdownMenuItem onClick={() => navigate(editHref)}>Editar perfil</DropdownMenuItem>
      <DropdownMenuSeparator />
      {perfil.ativo ? <DropdownMenuItem variant="destructive" onClick={() => alterar(false)}>Inativar perfil</DropdownMenuItem> : <DropdownMenuItem onClick={() => alterar(true)}>Ativar perfil</DropdownMenuItem>}
      <DropdownMenuItem variant="destructive" onClick={remover}>Excluir perfil</DropdownMenuItem>
    </DropdownMenuContent>
  </DropdownMenu>
}
