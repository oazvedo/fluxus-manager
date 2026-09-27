import { Ellipsis } from 'lucide-react'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { problemMessage } from '@/core/api/problem'
import { Button } from '@/shared/components/ui/button'
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from '@/shared/components/ui/dropdown-menu'
import { useAlterarStatusFilial } from '../hooks/use-filiais'
import type { Filial } from '../types/filial'

export function FilialActions({ filial, editHref }: { filial: Filial; editHref: string }) {
  const navigate = useNavigate()
  const alterarStatus = useAlterarStatusFilial()
  function alterar(ativo: boolean, desfazer = true) {
    alterarStatus.mutate({ id: filial.id, ativo }, { onSuccess: () => toast.success(`Filial ${filial.nome} ${ativo ? 'ativada' : 'inativada'}`, { action: desfazer ? { label: 'Desfazer', onClick: () => alterar(!ativo, false) } : undefined }), onError: (error) => toast.error(problemMessage(error)) })
  }
  return <DropdownMenu><DropdownMenuTrigger render={<Button variant="ghost" size="icon-sm" aria-label={`Ações de ${filial.nome}`} className="text-muted-foreground" />}><Ellipsis /></DropdownMenuTrigger><DropdownMenuContent align="end" className="w-44"><DropdownMenuItem onClick={() => navigate(editHref)}>Editar filial</DropdownMenuItem><DropdownMenuSeparator />{filial.ativo ? <DropdownMenuItem variant="destructive" onClick={() => alterar(false)}>Inativar filial</DropdownMenuItem> : <DropdownMenuItem onClick={() => alterar(true)}>Ativar filial</DropdownMenuItem>}</DropdownMenuContent></DropdownMenu>
}
