import { Moon, Sun } from 'lucide-react'
import { Button } from '@/shared/components/ui/button'
import { useTheme } from '@/core/theme/useTheme'

export function ThemeToggle() {
  const { theme, toggleTheme } = useTheme()
  const escuro = theme !== 'dark'
  const label = escuro ? 'Ativar tema escuro' : 'Ativar tema claro'

  return <Button type="button" variant="ghost" size="icon" aria-label={label} aria-pressed={theme === 'dark'}
    title={label} onClick={toggleTheme} className="text-muted-foreground">
    {theme === 'dark' ? <Sun aria-hidden="true" /> : <Moon aria-hidden="true" />}
  </Button>
}
