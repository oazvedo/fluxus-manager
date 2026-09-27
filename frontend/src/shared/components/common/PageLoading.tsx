import { Skeleton } from '@/shared/components/ui/skeleton'

export function PageLoading() {
  return (
    <main className="mx-auto w-full max-w-sm space-y-5 px-6 py-24" role="status" aria-label="Carregando página">
      <Skeleton className="h-8 w-40" />
      <Skeleton className="h-10 w-full" />
      <Skeleton className="h-10 w-full" />
    </main>
  )
}
