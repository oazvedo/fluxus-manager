import { useMutation } from '@tanstack/react-query'
import { Loader2, LogOut, Moon, Sun } from 'lucide-react'
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
 * Rodapé numa linha só: quem está na sessão e as duas ações dela (tema, sair) como ícones.
 * Com a barra recolhida, sobra o avatar, que abre um menu com as mesmas duas ações.
 */
export function SessionMenu() {
  const session = useSession()
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
      {/* Expandida */}
      <div className="flex items-center gap-2 px-1 group-data-[collapsible=icon]:hidden">
        <Avatar inicial={inicial} />
        <div className="grid min-w-0 flex-1 text-xs leading-tight" title={session?.email}>
          <span className="truncate font-medium text-sidebar-foreground">{session?.email}</span>
          <span className="truncate text-muted-foreground">{session?.role}</span>
        </div>
        <Tooltip>
          <TooltipTrigger
            render={<Button variant="ghost" size="icon-sm" aria-label={temaLabel} aria-pressed={escuro} onClick={toggleTheme}
              className={cn('text-muted-foreground hover:text-sidebar-foreground', pressable)} />}
          >
            {temaIcone}
          </TooltipTrigger>
          <TooltipContent side="top">{temaLabel}</TooltipContent>
        </Tooltip>
        <Tooltip>
          <TooltipTrigger
            render={<Button variant="ghost" size="icon-sm" aria-label="Sair da conta" onClick={() => logout.mutate()}
              disabled={logout.isPending} aria-busy={logout.isPending || undefined}
              className={cn('text-muted-foreground hover:text-sidebar-foreground', pressable)} />}
          >
            {sairIcone}
          </TooltipTrigger>
          <TooltipContent side="top">Sair da conta</TooltipContent>
        </Tooltip>
      </div>

      {/* Recolhida */}
      <DropdownMenu>
        <DropdownMenuTrigger
          render={<button type="button" aria-label={`Conta de ${session?.email ?? 'usuário'}`}
            className={cn('hidden self-center rounded-full outline-hidden ring-sidebar-ring focus-visible:ring-2 group-data-[collapsible=icon]:flex', pressable)} />}
        >
          <Avatar inicial={inicial} />
        </DropdownMenuTrigger>
        <DropdownMenuContent side="right" align="end" className="w-56">
          {/* No Base UI, o rótulo só existe dentro de um grupo. */}
          <DropdownMenuGroup>
            <DropdownMenuLabel className="truncate">{session?.email}</DropdownMenuLabel>
          </DropdownMenuGroup>
          <DropdownMenuSeparator />
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
