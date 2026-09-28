import { ThemeToggle } from '@/shared/components/common/ThemeToggle'

export function AuthBrand() {
  return <div className="flex items-center justify-between">
    <div className="flex items-center gap-2.5 text-sm font-semibold tracking-tight">
      <span aria-hidden="true" className="flex size-7 items-center justify-center rounded-md bg-foreground text-xs text-background">F</span>
      FluxusManager
    </div>
    <ThemeToggle />
  </div>
}
