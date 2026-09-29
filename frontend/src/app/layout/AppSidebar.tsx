import { NavLink, useLocation } from 'react-router'
import {
  Sidebar,
  SidebarContent,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarRail,
} from '@/shared/components/ui/sidebar'
import { BrandSymbol, BrandWordmark } from '@/shared/components/brand/BrandMark'
import { menuPress } from '@/shared/lib/motion'
import { useSession } from '@/core/auth/session'
import { matchesNav, visibleNavigation } from './navigation'
import { SessionMenu } from './SessionMenu'

export function AppSidebar() {
  const { pathname } = useLocation()
  const groups = visibleNavigation(useSession()?.administradorPlataforma === true)

  return (
    <Sidebar variant="inset" collapsible="icon">
      <SidebarHeader>
        <SidebarMenu>
          <SidebarMenuItem>
            <SidebarMenuButton size="lg" tooltip="Início" aria-label="FluxusManager, ir para o Início"
              className={`hover:bg-transparent! ${menuPress}`} render={<NavLink to="/" />}>
              {/* Caixa de 32px: com a barra recolhida, sobra só o símbolo, centralizado no botão.
                  O `!` vence o `[&_svg]:size-4` do botão, que encolhia a marca para 16px. */}
              <span className="flex aspect-square size-8 items-center justify-center">
                <BrandSymbol className="size-7!" />
              </span>
              <BrandWordmark className="truncate text-[1.0625rem]!" />
            </SidebarMenuButton>
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarHeader>

      <SidebarContent>
        {groups.map((group) => (
          <SidebarGroup key={group.label}>
            <SidebarGroupLabel>{group.label}</SidebarGroupLabel>
            <SidebarGroupContent>
              <SidebarMenu>
                {group.items.map((item) => {
                  const active = matchesNav(item.to, pathname)
                  return (
                    <SidebarMenuItem key={item.to}>
                      <SidebarMenuButton
                        isActive={active}
                        tooltip={item.title}
                        className={`text-sidebar-foreground/80 hover:text-sidebar-foreground ${menuPress}`}
                        render={<NavLink to={item.to} />}
                      >
                        <item.icon aria-hidden="true" />
                        <span>{item.title}</span>
                      </SidebarMenuButton>
                    </SidebarMenuItem>
                  )
                })}
              </SidebarMenu>
            </SidebarGroupContent>
          </SidebarGroup>
        ))}
      </SidebarContent>

      <SessionMenu />
      <SidebarRail />
    </Sidebar>
  )
}
