import { ThemeToggle } from '@/shared/components/common/ThemeToggle'

export function AuthBrand() {
  return <div className="flex items-center justify-between">
    <div className="flex items-center gap-3 font-semibold tracking-tight">
      <span aria-hidden="true" className="flex size-8 items-center justify-center rounded-lg bg-foreground text-background">F</span>
      FluxusManager
    </div>
    <ThemeToggle />
  </div>
}
