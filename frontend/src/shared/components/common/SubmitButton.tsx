import { Loader2 } from 'lucide-react'
import type { ComponentProps } from 'react'
import { Button } from '@/shared/components/ui/button'
import { cn } from '@/shared/lib/utils'

/**
 * Botão de envio: habilitado até o envio começar. Durante o envio o indicador aparece sobre o texto, que fica
 * invisível mas continua ocupando o espaço (e sendo lido): o botão não muda de largura nem empurra os vizinhos.
 */
export function SubmitButton({ pending, children, className, ...props }: ComponentProps<typeof Button> & { pending: boolean }) {
  return (
    <Button type="submit" disabled={pending} aria-busy={pending || undefined} className={cn('relative', className)} {...props}>
      <span className={cn('inline-flex items-center gap-[inherit]', pending && 'opacity-0')}>{children}</span>
      {pending ? (
        <Loader2 className="absolute inset-0 m-auto animate-spin motion-reduce:animate-none" aria-hidden="true" />
      ) : null}
    </Button>
  )
}
