import { Moon, Sun } from 'lucide-react'
import { Button } from '@/shared/components/ui/button'
import { useTheme } from '@/core/theme/useTheme'
import { iconHidden, iconSwap } from '@/shared/lib/motion'
import { cn } from '@/shared/lib/utils'

export function ThemeToggle() {
  const { theme, toggleTheme } = useTheme()
  const escuro = theme === 'dark'
  const label = escuro ? 'Ativar tema claro' : 'Ativar tema escuro'

  return <Button type="button" variant="ghost" size="icon" aria-label={label} aria-pressed={escuro}
    title={label} onClick={toggleTheme} className="text-muted-foreground">
    <span aria-hidden="true" className="relative size-4 shrink-0">
      <Moon data-theme-icon="" className={cn('absolute inset-0', iconSwap, escuro && iconHidden)} />
      <Sun data-theme-icon="" className={cn('absolute inset-0', iconSwap, !escuro && iconHidden)} />
    </span>
  </Button>
}
