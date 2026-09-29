import { Lock } from 'lucide-react'
import { Link } from 'react-router'
import { buttonVariants } from '@/shared/components/ui/button'

/** Área da equipe Fluxus aberta por quem não é administrador da plataforma (pela URL ou por link antigo). */
export function SemPermissao() {
  return (
    <div className="mx-auto flex w-full max-w-md flex-col items-start gap-4 py-16">
      <Lock aria-hidden="true" className="size-5 text-muted-foreground" />
      <div className="space-y-1.5">
        <h1 className="text-xl font-semibold tracking-tight text-balance">Esta área é da equipe Fluxus</h1>
        <p className="text-sm text-pretty text-muted-foreground">
          As solicitações de cadastro de empresas só aparecem para administradores da plataforma. Perfis e
          permissões da sua empresa não dão esse acesso.
        </p>
      </div>
      <Link to="/" className={buttonVariants({ variant: 'outline' })}>Ir para o Início</Link>
    </div>
  )
}
