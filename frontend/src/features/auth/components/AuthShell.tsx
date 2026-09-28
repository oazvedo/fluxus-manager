import type { ReactNode, Ref } from 'react'
import { CircleAlert, CircleCheck } from 'lucide-react'
import { Link, type LinkProps } from 'react-router'
import { enterFromBelow } from '@/shared/lib/motion'
import { cn } from '@/shared/lib/utils'
import { AuthBrand } from './AuthBrand'

type AuthShellProps = {
  title: string
  description?: ReactNode
  /** Recebe o foco quando o conteúdo troca sem navegação (ex.: envio concluído). */
  headingRef?: Ref<HTMLHeadingElement>
  children: ReactNode
}

/** Moldura das telas públicas: marca no topo, título e conteúdo numa coluna estreita e centralizada. */
export function AuthShell({ title, description, headingRef, children }: AuthShellProps) {
  return (
    <main className="flex min-h-svh flex-col px-6 py-6 sm:py-8">
      <div className="mx-auto w-full max-w-sm">
        <AuthBrand />
      </div>
      {/* Remonta a cada tela (login, recuperação, redefinição): a entrada curta marca a troca sem distrair. */}
      <div className={cn('mx-auto flex w-full max-w-sm flex-1 flex-col justify-center gap-8 py-12', enterFromBelow)}>
        <div className="space-y-1.5">
          <h1 ref={headingRef} tabIndex={-1} className="text-xl font-semibold tracking-tight text-balance outline-none">
            {title}
          </h1>
          {description && <p className="text-sm text-pretty text-muted-foreground">{description}</p>}
        </div>
        {children}
      </div>
    </main>
  )
}

/** Link secundário das telas públicas, com foco visível. */
export function AuthLink({ className, ...props }: LinkProps) {
  return (
    <Link
      className={cn(
        'rounded-sm text-sm text-muted-foreground underline-offset-4 transition-colors duration-150',
        'hover:text-foreground hover:underline focus-visible:text-foreground focus-visible:outline-2',
        'focus-visible:outline-offset-2 focus-visible:outline-ring',
        className,
      )}
      {...props}
    />
  )
}

/** Mensagem do formulário inteiro: erro (role="alert") ou confirmação (role="status"). Nunca só pela cor. */
export function FormNotice({ tone, children }: { tone: 'error' | 'success'; children: ReactNode }) {
  const Icon = tone === 'error' ? CircleAlert : CircleCheck
  return (
    <div
      role={tone === 'error' ? 'alert' : 'status'}
      className={cn(
        'flex gap-2.5 rounded-lg border px-3 py-2.5 text-sm text-pretty text-foreground', enterFromBelow,
        tone === 'error' ? 'border-destructive/30 bg-destructive/5' : 'bg-muted/60',
      )}
    >
      <Icon
        aria-hidden="true"
        className={cn('mt-0.5 size-4 shrink-0', tone === 'error' ? 'text-destructive' : 'text-muted-foreground')}
      />
      <div className="space-y-1">{children}</div>
    </div>
  )
}
