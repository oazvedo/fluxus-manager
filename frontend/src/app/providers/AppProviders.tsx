import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useState, type ReactNode } from 'react'
import { Toaster } from '@/shared/components/ui/sonner'
import { TooltipProvider } from '@/shared/components/ui/tooltip'
import { SessionBoundary } from './SessionBoundary'
import { getProblem } from '@/core/api/problem'

export function AppProviders({ children }: { children: ReactNode }) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 30_000,
            retry: (count, error) => ![401, 403].includes(getProblem(error)?.status ?? 0) && count < 1,
            refetchOnWindowFocus: false,
          },
        },
      }),
  )

  return (
    <QueryClientProvider client={queryClient}>
      <SessionBoundary>
        <TooltipProvider>{children}</TooltipProvider>
      </SessionBoundary>
      <Toaster position="bottom-right" />
    </QueryClientProvider>
  )
}
