import { useEffect, useState, type ReactNode } from 'react'
import { ThemeContext } from './theme-context'
import { applyTheme, getInitialTheme, saveTheme } from './theme-utils'

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setTheme] = useState(getInitialTheme)

  useEffect(() => {
    // Só o ícone do botão de tema anima. Sem isto, cada transição de cor da página dispara junto e a troca de tema "escorre" em vez de trocar de uma vez.
    const pause = document.createElement('style')
    pause.textContent = '*:not([data-theme-icon]),*::before,*::after{transition:none!important}'
    document.head.appendChild(pause)
    applyTheme(theme)
    void document.body.offsetHeight
    const frame = requestAnimationFrame(() => pause.remove())
    return () => {
      cancelAnimationFrame(frame)
      pause.remove()
    }
  }, [theme])

  function toggleTheme() {
    const nextTheme = theme === 'dark' ? 'light' : 'dark'
    setTheme(nextTheme)
    saveTheme(nextTheme)
  }

  return <ThemeContext.Provider value={{ theme, toggleTheme }}>
    {children}
  </ThemeContext.Provider>
}
