import { Hammer } from 'lucide-react'
import { Link } from 'react-router'
import { buttonVariants } from '@/shared/components/ui/button'
import { cn } from '@/shared/lib/utils'

/** Página provisória para módulos ainda não implementados. */
export function ComingSoon({ title }: { title: string }) {
  return (
    <div className="mx-auto flex max-w-md flex-col items-center justify-center gap-4 py-24 text-center">
      <div className="flex size-12 items-center justify-center rounded-xl border bg-muted/40 text-muted-foreground">
        <Hammer className="size-5" />
      </div>
      <div className="space-y-1">
        <h1 className="text-lg font-semibold tracking-tight">{title}</h1>
        <p className="text-sm text-muted-foreground">Este módulo está em desenvolvimento e ficará disponível em breve.</p>
      </div>
      <Link to="/" className={cn(buttonVariants({ variant: 'outline', size: 'sm' }))}>
        Voltar ao início
      </Link>
    </div>
  )
}
