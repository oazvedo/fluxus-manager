import { Link } from 'react-router'
import { buttonVariants } from '@/shared/components/ui/button'
import { cn } from '@/shared/lib/utils'

export function NotFound() {
  return (
    <div className="mx-auto flex max-w-md flex-col items-center justify-center gap-4 py-24 text-center">
      <p className="text-5xl font-semibold tracking-tight text-muted-foreground/40">404</p>
      <div className="space-y-1">
        <h1 className="text-lg font-semibold tracking-tight">Página não encontrada</h1>
        <p className="text-sm text-muted-foreground">Confira o endereço digitado ou volte ao início.</p>
      </div>
      <Link to="/" className={cn(buttonVariants({ variant: 'outline', size: 'sm' }))}>
        Voltar ao início
      </Link>
    </div>
  )
}
