import { Building2, ChevronRight, MailPlus, ShieldCheck, Store, Users, type LucideIcon } from 'lucide-react'
import { Link } from 'react-router'
import { Card } from '@/shared/components/ui/card'

type Module = { title: string; description: string; to: string; icon: LucideIcon }

const modules: Module[] = [
  { title: 'Empresas', description: 'Cadastro das empresas e seus dados fiscais.', to: '/empresas', icon: Building2 },
  { title: 'Filiais', description: 'Unidades vinculadas a cada empresa.', to: '/filiais', icon: Store },
  { title: 'Usuários', description: 'Pessoas com acesso e seus vínculos com as empresas.', to: '/usuarios', icon: Users },
  { title: 'Perfis e permissões', description: 'O que cada perfil pode ver e fazer no sistema.', to: '/perfis', icon: ShieldCheck },
  { title: 'Convites', description: 'Convites por e-mail para acessar a empresa.', to: '/convites', icon: MailPlus },
]

export function ModuleList() {
  return (
    <section aria-labelledby="cadastros" className="space-y-3">
      <h2 id="cadastros" className="text-sm font-medium text-muted-foreground">
        Cadastros
      </h2>
      <Card className="gap-0 py-0">
        <ul className="divide-y">
          {modules.map((module) => (
            <li key={module.to}>
              <Link
                to={module.to}
                className="group flex items-center gap-3 px-4 py-3 outline-none transition-colors hover:bg-muted/60 focus-visible:bg-muted/60 focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-inset"
              >
                <module.icon aria-hidden="true" className="size-4 shrink-0 text-muted-foreground" />
                <span className="min-w-0 flex-1 sm:flex sm:items-baseline sm:gap-3">
                  <span className="block text-sm font-medium sm:w-44 sm:shrink-0">{module.title}</span>
                  <span className="block truncate text-sm text-muted-foreground">{module.description}</span>
                </span>
                <ChevronRight
                  aria-hidden="true"
                  className="size-4 shrink-0 text-muted-foreground transition-transform group-hover:translate-x-0.5 motion-reduce:transition-none"
                />
              </Link>
            </li>
          ))}
        </ul>
      </Card>
    </section>
  )
}
