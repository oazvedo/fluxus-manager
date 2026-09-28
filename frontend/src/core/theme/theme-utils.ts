export type Theme = 'light' | 'dark'

const themeStorageKey = 'fluxus.theme.v1'

export function getInitialTheme(): Theme {
  if (typeof window === 'undefined') return 'light'
  try {
    const stored = window.localStorage.getItem(themeStorageKey)
    if (stored === 'light' || stored === 'dark') return stored
  } catch {
    // Sem acesso ao armazenamento, segue o tema do sistema.
  }
  return window.matchMedia?.('(prefers-color-scheme: dark)')?.matches ? 'dark' : 'light'
}

export function saveTheme(theme: Theme) {
  try {
    window.localStorage.setItem(themeStorageKey, theme)
  } catch {
    // O tema segue disponível durante esta aba, mesmo sem persistência.
  }
}

export function applyTheme(theme: Theme) {
  if (typeof document === 'undefined') return
  document.documentElement.classList.toggle('dark', theme === 'dark')
  document.documentElement.style.colorScheme = theme
}
