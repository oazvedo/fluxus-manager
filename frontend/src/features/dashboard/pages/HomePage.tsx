import { GettingStartedCard } from '../components/GettingStartedCard'
import { ModuleGrid } from '../components/ModuleGrid'
import { SystemStatusCard } from '../components/SystemStatusCard'
import { WelcomeHeader } from '../components/WelcomeHeader'

export function HomePage() {
  return (
    <div className="mx-auto w-full max-w-6xl space-y-8">
      <WelcomeHeader />

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="lg:col-span-2">
          <ModuleGrid />
        </div>
        <div className="space-y-6 lg:pt-8">
          <SystemStatusCard />
          <GettingStartedCard />
        </div>
      </div>
    </div>
  )
}
