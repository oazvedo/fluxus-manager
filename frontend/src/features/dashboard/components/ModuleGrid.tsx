import { ArrowUpRight, Building2, ShieldCheck, Store, Users, type LucideIcon } from 'lucide-react'
import { Link } from 'react-router'
import { Badge } from '@/shared/components/ui/badge'
import { Card, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card'

type Module = { title: string; description: string; to: string; icon: LucideIcon; available: boolean }

const modules: Module[] = [
  {
    title: 'Empresas',
    description: 'Cadastro das empresas e seus dados fiscais.',
    to: '/empresas',
    icon: Building2,
    available: true,
  },
  {
    title: 'Filiais',
    description: 'Unidades vinculadas a cada empresa.',
    to: '/filiais',
    icon: Store,
    available: true,
  },
  {
    title: 'Usuários',
    description: 'Pessoas com acesso e seus vínculos com as empresas.',
    to: '/usuarios',
    icon: Users,
    available: true,
  },
  {
    title: 'Perfis e permissões',
    description: 'O que cada perfil pode ver e fazer no sistema.',
    to: '/perfis',
    icon: ShieldCheck,
    available: false,
  },
]

export function ModuleGrid() {
  return (
    <section className="space-y-3">
      <h2 className="text-sm font-medium text-muted-foreground">Módulos</h2>
      <div className="grid gap-4 sm:grid-cols-2">
        {modules.map((module) => (
          <Link key={module.to} to={module.to} className="group rounded-xl outline-none focus-visible:ring-2 focus-visible:ring-ring">
            <Card className="h-full transition-colors group-hover:bg-muted/40">
              <CardHeader className="gap-3">
                <div className="flex items-start justify-between">
                  <div className="flex size-9 items-center justify-center rounded-lg border bg-muted/40 text-foreground">
                    <module.icon className="size-4" />
                  </div>
                  {module.available ? (
                    <ArrowUpRight className="size-4 text-muted-foreground transition-transform group-hover:-translate-y-0.5 group-hover:translate-x-0.5" />
                  ) : (
                    <Badge variant="outline" className="text-muted-foreground">
                      Em breve
                    </Badge>
                  )}
                </div>
                <div className="space-y-1">
                  <CardTitle>{module.title}</CardTitle>
                  <CardDescription>{module.description}</CardDescription>
                </div>
              </CardHeader>
            </Card>
          </Link>
        ))}
      </div>
    </section>
  )
}
