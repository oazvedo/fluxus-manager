import { Outlet, useLocation, useMatches } from 'react-router'
import type { RouteHandle } from '@/app/router/router'
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from '@/shared/components/ui/breadcrumb'
import { Separator } from '@/shared/components/ui/separator'
import { SidebarInset, SidebarProvider, SidebarTrigger } from '@/shared/components/ui/sidebar'
import { AppSidebar } from './AppSidebar'
import { findNavGroup } from './navigation'

function usePageTitle() {
  const matches = useMatches()
  const handle = matches.at(-1)?.handle as RouteHandle | undefined
  return handle?.title ?? ''
}

// Abrir e recolher a barra lateral com curva de gaveta (sai rápido, assenta devagar); instantâneo com movimento reduzido.
const sidebarMotion = [
  '[&_[data-slot=sidebar-gap]]:duration-[240ms] [&_[data-slot=sidebar-gap]]:ease-[cubic-bezier(0.32,0.72,0,1)]',
  '[&_[data-slot=sidebar-container]]:duration-[240ms] [&_[data-slot=sidebar-container]]:ease-[cubic-bezier(0.32,0.72,0,1)]',
  'motion-reduce:[&_[data-slot=sidebar-gap]]:duration-0 motion-reduce:[&_[data-slot=sidebar-container]]:duration-0',
].join(' ')

export function AppLayout() {
  const title = usePageTitle()
  const { pathname } = useLocation()
  const group = findNavGroup(pathname)

  return (
    <SidebarProvider className={sidebarMotion}>
      <title>{title ? `${title} · FluxusManager` : 'FluxusManager'}</title>
      <a
        href="#conteudo"
        className="sr-only rounded-md bg-background px-3 py-2 text-sm font-medium shadow-sm ring-2 ring-ring focus:not-sr-only focus:fixed focus:top-3 focus:left-3 focus:z-50"
      >
        Pular para o conteúdo
      </a>
      <AppSidebar />
      <SidebarInset>
        <header className="flex h-12 shrink-0 items-center gap-2 border-b px-4">
          <SidebarTrigger className="-ml-1 text-muted-foreground hover:text-foreground" />
          <Separator orientation="vertical" className="mr-2 data-vertical:h-4 data-vertical:self-center" />
          <Breadcrumb>
            <BreadcrumbList>
              {group && group.items.length > 1 && (
                <>
                  <BreadcrumbItem className="hidden md:block">{group.label}</BreadcrumbItem>
                  <BreadcrumbSeparator className="hidden md:block" />
                </>
              )}
              <BreadcrumbItem>
                <BreadcrumbPage className="font-medium">{title}</BreadcrumbPage>
              </BreadcrumbItem>
            </BreadcrumbList>
          </Breadcrumb>
        </header>

        <div id="conteudo" tabIndex={-1} className="flex-1 p-4 outline-none md:p-8">
          <Outlet />
        </div>
      </SidebarInset>
    </SidebarProvider>
  )
}
