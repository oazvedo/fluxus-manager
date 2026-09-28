import { useEffect, useState, type ReactNode } from 'react'
import { ThemeContext } from './theme-context'
import { applyTheme, getInitialTheme, saveTheme } from './theme-utils'

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setTheme] = useState(getInitialTheme)

  useEffect(() => applyTheme(theme), [theme])

  function toggleTheme() {
    const nextTheme = theme === 'dark' ? 'light' : 'dark'
    setTheme(nextTheme)
    saveTheme(nextTheme)
  }

  return <ThemeContext.Provider value={{ theme, toggleTheme }}>
    {children}
  </ThemeContext.Provider>
}
