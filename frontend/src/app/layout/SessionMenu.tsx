import { useMutation } from '@tanstack/react-query'
import { KeyRound, Loader2, LogOut, Moon, Sun } from 'lucide-react'
import { useNavigate } from 'react-router'
import { notify } from '@/shared/lib/notify'
import { sair } from '@/core/auth/actions'
import { useSession } from '@/core/auth/session'
import { useTheme } from '@/core/theme/useTheme'
import { Button } from '@/shared/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/shared/components/ui/dropdown-menu'
import { SidebarFooter } from '@/shared/components/ui/sidebar'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/shared/components/ui/tooltip'
import { iconHidden, iconSwap, pressable } from '@/shared/lib/motion'
import { cn } from '@/shared/lib/utils'

/*
 * O perfil abre o menu da conta tanto com a barra expandida quanto recolhida.
 * Tema e sair continuam disponíveis como ações rápidas quando há espaço.
 */
export function SessionMenu() {
  const session = useSession()
  const navigate = useNavigate()
  const { theme, toggleTheme } = useTheme()
  const escuro = theme === 'dark'
  const temaLabel = escuro ? 'Ativar tema claro' : 'Ativar tema escuro'
  const logout = useMutation({
    mutationFn: sair,
    retry: false,
    onError: () => notify.error('Você saiu desta aba, mas não foi possível encerrar a sessão no servidor. Verifique sua conexão.'),
  })
  const inicial = session?.email.charAt(0).toUpperCase() ?? '?'

  const temaIcone = (
    <span aria-hidden="true" className="relative size-4 shrink-0">
      <Moon data-theme-icon="" className={cn('absolute inset-0', iconSwap, escuro && iconHidden)} />
      <Sun data-theme-icon="" className={cn('absolute inset-0', iconSwap, !escuro && iconHidden)} />
    </span>
  )
  const sairIcone = logout.isPending
    ? <Loader2 aria-hidden="true" className="animate-spin motion-reduce:animate-none" />
    : <LogOut aria-hidden="true" />

  return (
    <SidebarFooter>
      <DropdownMenu>
        <div className="flex items-center gap-2 px-1">
          <DropdownMenuTrigger
            render={<button type="button" aria-label={`Abrir opções da conta de ${session?.email ?? 'usuário'}`}
              className={cn(
                'flex min-w-0 flex-1 items-center gap-2 rounded-md text-left outline-hidden ring-sidebar-ring focus-visible:ring-2',
                'group-data-[collapsible=icon]:size-8 group-data-[collapsible=icon]:flex-none group-data-[collapsible=icon]:justify-center',
                pressable,
              )} />}
          >
            <Avatar inicial={inicial} />
            <div className="grid min-w-0 flex-1 text-xs leading-tight group-data-[collapsible=icon]:hidden" title={session?.email}>
              <span className="truncate font-medium text-sidebar-foreground">{session?.email}</span>
              <span className="truncate text-muted-foreground">{session?.role}</span>
            </div>
          </DropdownMenuTrigger>
          <Tooltip>
            <TooltipTrigger
              render={<Button variant="ghost" size="icon-sm" aria-label={temaLabel} aria-pressed={escuro} onClick={toggleTheme}
                className={cn('text-muted-foreground hover:text-sidebar-foreground group-data-[collapsible=icon]:hidden', pressable)} />}
            >
              {temaIcone}
            </TooltipTrigger>
            <TooltipContent side="top">{temaLabel}</TooltipContent>
          </Tooltip>
          <Tooltip>
            <TooltipTrigger
              render={<Button variant="ghost" size="icon-sm" aria-label="Sair da conta" onClick={() => logout.mutate()}
                disabled={logout.isPending} aria-busy={logout.isPending || undefined}
                className={cn('text-muted-foreground hover:text-sidebar-foreground group-data-[collapsible=icon]:hidden', pressable)} />}
            >
              {sairIcone}
            </TooltipTrigger>
            <TooltipContent side="top">Sair da conta</TooltipContent>
          </Tooltip>
        </div>
        <DropdownMenuContent side="top" align="end" className="w-56">
          <DropdownMenuGroup>
            <DropdownMenuLabel className="truncate">{session?.email}</DropdownMenuLabel>
          </DropdownMenuGroup>
          <DropdownMenuSeparator />
          <DropdownMenuItem onClick={() => navigate('/conta/trocar-senha')}>
            <KeyRound aria-hidden="true" />
            Trocar senha
          </DropdownMenuItem>
          <DropdownMenuItem onClick={toggleTheme}>{temaIcone}{temaLabel}</DropdownMenuItem>
          <DropdownMenuItem onClick={() => logout.mutate()} disabled={logout.isPending}>{sairIcone}Sair da conta</DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    </SidebarFooter>
  )
}

function Avatar({ inicial }: { inicial: string }) {
  return (
    <span aria-hidden="true"
      className="flex size-8 shrink-0 items-center justify-center rounded-full bg-sidebar-accent text-xs font-semibold text-sidebar-accent-foreground">
      {inicial}
    </span>
  )
}
