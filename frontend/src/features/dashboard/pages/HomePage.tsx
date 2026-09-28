import { PageHeader } from '@/shared/components/common/PageHeader'
import { CadastrosRecentes } from '../components/CadastrosRecentes'
import { ConvitesAtencao } from '../components/ConvitesAtencao'
import { QuickActions } from '../components/QuickActions'
import { SystemStatus } from '../components/SystemStatus'

const dateFormat = new Intl.DateTimeFormat('pt-BR', { weekday: 'long', day: 'numeric', month: 'long' })

/** Início: o que pede ação (convites vencendo) e o que acabou de ser feito. A navegação fica na barra lateral. */
export function HomePage() {
  const now = new Date()

  return (
    <div className="mx-auto w-full max-w-3xl space-y-10">
      <PageHeader
        title="Início"
        meta={
          <time dateTime={now.toLocaleDateString('sv-SE')} className="inline-block first-letter:uppercase">
            {dateFormat.format(now)}
          </time>
        }
      />
      <QuickActions />
      <ConvitesAtencao />
      <CadastrosRecentes />
      <SystemStatus />
    </div>
  )
}
