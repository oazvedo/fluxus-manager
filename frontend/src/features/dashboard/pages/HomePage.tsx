import { PageHeader } from '@/shared/components/common/PageHeader'
import { ModuleList } from '../components/ModuleList'
import { QuickActions } from '../components/QuickActions'
import { SystemStatus } from '../components/SystemStatus'

const dateFormat = new Intl.DateTimeFormat('pt-BR', { weekday: 'long', day: 'numeric', month: 'long' })

export function HomePage() {
  const now = new Date()

  return (
    <div className="mx-auto w-full max-w-4xl space-y-8">
      <PageHeader
        title="Início"
        meta={
          <time dateTime={now.toLocaleDateString('sv-SE')} className="inline-block first-letter:uppercase">
            {dateFormat.format(now)}
          </time>
        }
      />
      <QuickActions />
      <ModuleList />
      <SystemStatus />
    </div>
  )
}
