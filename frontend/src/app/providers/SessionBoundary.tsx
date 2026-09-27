import { useEffect, type ReactNode } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { getSession, subscribeSession } from '@/core/auth/session'

/** Impede dados da sessão anterior de permanecerem no cache após sair ou entrar. */
export function SessionBoundary({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  useEffect(() => {
    let id = getSession()?.id
    return subscribeSession(() => {
      const nextId = getSession()?.id
      if (id !== nextId) {
        id = nextId
        queryClient.clear()
      }
    })
  }, [queryClient])
  return children
}
