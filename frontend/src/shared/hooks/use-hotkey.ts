import { useEffect, useEffectEvent } from 'react'

function isTyping(target: EventTarget | null) {
  if (!(target instanceof HTMLElement)) return false
  return target.isContentEditable || ['INPUT', 'TEXTAREA', 'SELECT'].includes(target.tagName)
}

/**
 * Atalho de uma tecla (ex.: "n" para nova empresa). Ignorado enquanto o usuário digita num campo,
 * com Ctrl/Cmd/Alt pressionado ou quando `enabled` é false (ex.: painel aberto).
 */
export function useHotkey(key: string, handler: () => void, enabled = true) {
  const onHotkey = useEffectEvent(handler)

  useEffect(() => {
    if (!enabled) return

    function onKeyDown(event: KeyboardEvent) {
      if (event.key.toLowerCase() !== key || event.ctrlKey || event.metaKey || event.altKey) return
      if (event.repeat || isTyping(event.target)) return
      event.preventDefault()
      onHotkey()
    }

    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [key, enabled])
}
