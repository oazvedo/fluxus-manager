import { cn } from '@/shared/lib/utils'

/** Status ativo/inativo: ponto colorido + texto, nunca só a cor. */
export function StatusBadge({ active, labels }: { active: boolean; labels: { active: string; inactive: string } }) {
  return (
    <span className={cn('inline-flex items-center gap-2 text-sm', !active && 'text-muted-foreground')}>
      <span
        aria-hidden="true"
        className={cn('size-1.5 rounded-full', active ? 'bg-success' : 'bg-muted-foreground/60')}
      />
      {active ? labels.active : labels.inactive}
    </span>
  )
}
