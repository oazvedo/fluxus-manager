import { Button } from '@/shared/components/ui/button'
import { notify } from '@/shared/lib/notify'
import { pressable } from '@/shared/lib/motion'
import { cn } from '@/shared/lib/utils'
import { useCooldown } from '../hooks/use-cooldown'
import { useReenviarVerificacao } from '../hooks/use-solicitacao'

const ESPERA_SEGUNDOS = 60

/** Pede outro link de confirmação para o e-mail. A resposta é neutra; o aviso também. */
export function ReenviarLink({ email, className }: { email: string; className?: string }) {
  const reenviar = useReenviarVerificacao()
  const [restante, iniciarEspera] = useCooldown(ESPERA_SEGUNDOS)
  const aguardando = restante > 0

  function reenviarLink() {
    reenviar.mutate(email, {
      onSuccess: () => {
        iniciarEspera()
        notify.success('Pedido de novo link enviado', { description: `Se houver solicitação para ${email}, o link chega em instantes.` })
      },
      onError: (error) => notify.error(error),
    })
  }

  return (
    <Button type="button" variant="outline" size="lg" className={cn('w-full tabular-nums', pressable, className)}
      disabled={reenviar.isPending || aguardando} aria-busy={reenviar.isPending || undefined} onClick={reenviarLink}>
      {aguardando ? `Reenviar link em ${restante} s` : 'Reenviar link'}
    </Button>
  )
}
