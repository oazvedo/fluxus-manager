import { Plus } from 'lucide-react'
import { Link } from 'react-router'
import { buttonVariants } from '@/shared/components/ui/button'

// "?novo" abre o painel de cadastro direto na tela da lista (ver useRecordParams).
const actions = [
  { label: 'Nova empresa', to: '/empresas?novo' },
  { label: 'Nova filial', to: '/filiais?novo' },
  { label: 'Novo usuário', to: '/usuarios?novo' },
  { label: 'Novo convite', to: '/convites?novo' },
]

export function QuickActions() {
  return (
    <section aria-labelledby="acoes-rapidas" className="space-y-3">
      <h2 id="acoes-rapidas" className="text-sm font-medium text-muted-foreground">
        Ações rápidas
      </h2>
      <div className="flex flex-wrap gap-2">
        {actions.map((action, index) => (
          <Link
            key={action.to}
            to={action.to}
            className={buttonVariants({ variant: index === 0 ? 'default' : 'outline', size: 'lg' })}
          >
            <Plus aria-hidden="true" />
            {action.label}
          </Link>
        ))}
      </div>
    </section>
  )
}
