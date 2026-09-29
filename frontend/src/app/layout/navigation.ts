import { Building2, House, Inbox, MailPlus, ShieldCheck, Store, Users, type LucideIcon } from 'lucide-react'

export type NavItem = { title: string; to: string; icon: LucideIcon }
/** `plataforma`: grupo da equipe Fluxus, só para administradores da plataforma. */
export type NavGroup = { label: string; items: NavItem[]; plataforma?: true }

export const navigation: NavGroup[] = [
  {
    label: 'Geral',
    items: [{ title: 'Início', to: '/', icon: House }],
  },
  {
    label: 'Cadastros',
    items: [
      { title: 'Empresas', to: '/empresas', icon: Building2 },
      { title: 'Filiais', to: '/filiais', icon: Store },
      { title: 'Usuários', to: '/usuarios', icon: Users },
    ],
  },
  {
    label: 'Acesso',
    items: [
      { title: 'Perfis e permissões', to: '/perfis', icon: ShieldCheck },
      { title: 'Convites', to: '/convites', icon: MailPlus },
    ],
  },
  {
    label: 'Plataforma',
    plataforma: true,
    items: [{ title: 'Solicitações de cadastro', to: '/plataforma/solicitacoes', icon: Inbox }],
  },
]

/** Item de menu correspondente ao caminho: exato em "/", e por segmento nas demais (não confunde /empresas com /empresasX). */
export function matchesNav(to: string, pathname: string) {
  return to === '/' ? pathname === '/' : pathname === to || pathname.startsWith(`${to}/`)
}

/** Grupos que a pessoa vê: o da plataforma só com o papel global. */
export function visibleNavigation(administradorPlataforma: boolean) {
  return navigation.filter((group) => !group.plataforma || administradorPlataforma)
}

export function findNavGroup(pathname: string) {
  return navigation.find((group) => group.items.some((item) => matchesNav(item.to, pathname)))
}
