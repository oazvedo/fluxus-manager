import { Loader2 } from 'lucide-react'
import type { ComponentProps } from 'react'
import { Button } from '@/shared/components/ui/button'

/** Botão de envio: habilitado até o envio começar; durante o envio mantém o texto e mostra o indicador. */
export function SubmitButton({ pending, children, ...props }: ComponentProps<typeof Button> & { pending: boolean }) {
  return (
    <Button type="submit" disabled={pending} aria-busy={pending || undefined} {...props}>
      {pending ? <Loader2 className="animate-spin motion-reduce:animate-none" aria-hidden="true" /> : null}
      {children}
    </Button>
  )
}
