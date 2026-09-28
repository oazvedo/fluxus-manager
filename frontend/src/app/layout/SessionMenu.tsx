import { useMutation } from '@tanstack/react-query'
import { Loader2, LogOut, Moon, Sun } from 'lucide-react'
import { notify } from '@/shared/lib/notify'
import { sair } from '@/core/auth/actions'
import { useSession } from '@/core/auth/session'
import { useTheme } from '@/core/theme/useTheme'
import { SidebarFooter, SidebarMenu, SidebarMenuItem, SidebarMenuButton } from '@/shared/components/ui/sidebar'
import { iconHidden, iconSwap, menuPress } from '@/shared/lib/motion'
import { cn } from '@/shared/lib/utils'

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

  return (
    <SidebarFooter>
      <div className="flex items-center gap-2 rounded-md p-2 group-data-[collapsible=icon]:p-0" title={session?.email}>
        <span aria-hidden="true"
          className="flex size-8 shrink-0 items-center justify-center rounded-md bg-sidebar-accent text-xs font-semibold text-sidebar-accent-foreground">
          {inicial}
        </span>
        <div className="grid min-w-0 flex-1 text-xs leading-tight group-data-[collapsible=icon]:hidden">
          <span className="truncate font-medium text-sidebar-foreground">{session?.email}</span>
          <span className="truncate text-muted-foreground">{session?.role}</span>
        </div>
      </div>
      <SidebarMenu>
        <SidebarMenuItem>
          <SidebarMenuButton tooltip={temaLabel} aria-label={temaLabel} aria-pressed={escuro} onClick={toggleTheme}
            className={menuPress}>
            <span aria-hidden="true" className="relative size-4 shrink-0">
              <Moon data-theme-icon="" className={cn('absolute inset-0', iconSwap, escuro && iconHidden)} />
              <Sun data-theme-icon="" className={cn('absolute inset-0', iconSwap, !escuro && iconHidden)} />
            </span>
            <span>{escuro ? 'Tema claro' : 'Tema escuro'}</span>
          </SidebarMenuButton>
        </SidebarMenuItem>
        <SidebarMenuItem>
          <SidebarMenuButton tooltip="Sair da conta" onClick={() => logout.mutate()} disabled={logout.isPending}
            aria-busy={logout.isPending || undefined} className={menuPress}>
            {logout.isPending
              ? <Loader2 aria-hidden="true" className="animate-spin motion-reduce:animate-none" />
              : <LogOut aria-hidden="true" />}
            <span>Sair da conta</span>
          </SidebarMenuButton>
        </SidebarMenuItem>
      </SidebarMenu>
    </SidebarFooter>
  )
}
