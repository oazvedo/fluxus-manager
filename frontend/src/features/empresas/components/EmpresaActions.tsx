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
import { useAlterarStatusEmpresa } from '../hooks/use-empresas'
import type { Empresa } from '../types/empresa'

/** Menu "⋯" da linha: editar e ativar/inativar. Inativar não pede confirmação: o aviso oferece desfazer. */
export function EmpresaActions({ empresa, editHref }: { empresa: Empresa; editHref: string }) {
  const navigate = useNavigate()
  const alterarStatus = useAlterarStatusEmpresa()
  // Mesmo nome da primeira coluna da tabela, para o aviso apontar para a linha que a pessoa vê.
  const nome = empresa.razaoSocial

  function alterar(ativo: boolean, desfazer = true) {
    alterarStatus.mutate(
      { id: empresa.id, ativo },
      {
        onSuccess: () =>
          notify.success(ativo ? `${nome} ativada` : `${nome} inativada`, {
            undo: desfazer ? () => alterar(!ativo, false) : undefined,
          }),
        onError: (error) => notify.error(error),
      },
    )
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger
        render={<Button variant="ghost" size="icon-sm" aria-label={`Ações de ${nome}`} className="text-muted-foreground" />}
      >
        <Ellipsis />
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-44">
        <DropdownMenuItem onClick={() => navigate(editHref)}>Editar empresa</DropdownMenuItem>
        <DropdownMenuSeparator />
        {empresa.ativo ? (
          <DropdownMenuItem onClick={() => alterar(false)}>
            Inativar empresa
          </DropdownMenuItem>
        ) : (
          <DropdownMenuItem onClick={() => alterar(true)}>Ativar empresa</DropdownMenuItem>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
