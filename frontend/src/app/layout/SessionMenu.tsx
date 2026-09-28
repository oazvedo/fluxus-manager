import { useMutation } from '@tanstack/react-query'
import { LogOut, Moon, Sun } from 'lucide-react'
import { toast } from 'sonner'
import { sair } from '@/core/auth/actions'
import { useSession } from '@/core/auth/session'
import { useTheme } from '@/core/theme/useTheme'
import { SidebarFooter, SidebarMenu, SidebarMenuItem, SidebarMenuButton } from '@/shared/components/ui/sidebar'

export function SessionMenu() {
  const session = useSession()
  const { theme, toggleTheme } = useTheme()
  const logout = useMutation({
    mutationFn: sair,
    retry: false,
    onError: () => toast.error('Você saiu desta aba, mas não foi possível encerrar a sessão no servidor. Verifique sua conexão.'),
  })
  return (
    <SidebarFooter>
      <div className="px-2 py-1 text-xs text-muted-foreground group-data-[collapsible=icon]:hidden">
        <p className="truncate" title={session?.email}>{session?.email}</p>
        <p className="truncate">{session?.role}</p>
      </div>
      <SidebarMenu>
        <SidebarMenuItem>
          <SidebarMenuButton tooltip={theme === 'dark' ? 'Ativar tema claro' : 'Ativar tema escuro'}
            aria-label={theme === 'dark' ? 'Ativar tema claro' : 'Ativar tema escuro'}
            aria-pressed={theme === 'dark'} onClick={toggleTheme}>
            {theme === 'dark' ? <Sun aria-hidden="true" /> : <Moon aria-hidden="true" />}
            <span>{theme === 'dark' ? 'Tema claro' : 'Tema escuro'}</span>
          </SidebarMenuButton>
        </SidebarMenuItem>
        <SidebarMenuItem>
          <SidebarMenuButton tooltip="Sair da conta" onClick={() => logout.mutate()} disabled={logout.isPending}>
            <LogOut aria-hidden="true" /><span>Sair da conta</span>
          </SidebarMenuButton>
        </SidebarMenuItem>
      </SidebarMenu>
    </SidebarFooter>
  )
}
