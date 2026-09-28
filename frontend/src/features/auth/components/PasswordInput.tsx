import { useState, type ComponentProps } from 'react'
import { Eye, EyeOff } from 'lucide-react'
import { Button } from '@/shared/components/ui/button'
import { Input } from '@/shared/components/ui/input'
import { iconHidden, iconSwap, pressable } from '@/shared/lib/motion'
import { cn } from '@/shared/lib/utils'

/** Campo de senha com botão para mostrar o que foi digitado. */
export function PasswordInput({ className, ...props }: Omit<ComponentProps<typeof Input>, 'type'>) {
  const [visivel, setVisivel] = useState(false)

  return (
    <div className="relative">
      <Input {...props} type={visivel ? 'text' : 'password'} className={cn('pr-9', className)} />
      <Button
        type="button"
        variant="ghost"
        size="icon-sm"
        aria-label="Mostrar senha"
        aria-pressed={visivel}
        aria-controls={props.id}
        onClick={() => setVisivel((atual) => !atual)}
        className={cn('absolute top-0.5 right-0.5 size-7 text-muted-foreground', pressable)}
      >
        <span aria-hidden="true" className="relative size-4">
          <Eye className={cn('absolute inset-0', iconSwap, visivel && iconHidden)} />
          <EyeOff className={cn('absolute inset-0', iconSwap, !visivel && iconHidden)} />
        </span>
      </Button>
    </div>
  )
}
