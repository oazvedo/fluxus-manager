import { Building2, Store, Users, type LucideIcon } from 'lucide-react'
import { Link } from 'react-router'
import { Skeleton } from '@/shared/components/ui/skeleton'
import { formatDateTime } from '@/shared/lib/format'
import type { TipoCadastro } from '../api/inicio'
import { useCadastrosRecentes } from '../hooks/useInicio'
import { formatRelative } from '../lib/relative-time'
import { InicioSection } from './InicioSection'

const tipos: Record<TipoCadastro, { label: string; path: string; icon: LucideIcon }> = {
  empresa: { label: 'Empresa', path: '/empresas', icon: Building2 },
  filial: { label: 'Filial', path: '/filiais', icon: Store },
  usuario: { label: 'Usuário', path: '/usuarios', icon: Users },
}

/** Últimos cadastros de empresas, filiais e usuários: o jeito mais curto de voltar ao que acabou de ser feito. */
export function CadastrosRecentes() {
  const { data, isPending, isError } = useCadastrosRecentes()

  return (
    <InicioSection id="cadastros-recentes" title="Cadastrados por último">
      {isPending ? (
        <div className="space-y-2 py-2" aria-busy="true">
          {Array.from({ length: 4 }, (_, i) => (
            <Skeleton key={i} className="h-5 w-full" />
          ))}
        </div>
      ) : isError ? (
        <p role="alert" className="py-3 text-sm text-destructive">
          Não foi possível carregar os cadastros recentes. Atualize a página para tentar de novo.
        </p>
      ) : data.length === 0 ? (
        <p className="py-3 text-sm text-muted-foreground">
          Nenhum cadastro ainda. Comece pela empresa: filiais e usuários são vinculados a ela.
        </p>
      ) : (
        <ul className="divide-y">
          {data.map((item) => {
            const tipo = tipos[item.tipo]
            return (
              <li key={`${item.tipo}-${item.id}`}>
                <Link
                  to={`${tipo.path}?editar=${item.id}`}
                  className="-mx-2 flex items-center gap-3 rounded-md px-2 py-2.5 text-sm outline-none transition-colors hover:bg-muted/60 focus-visible:ring-2 focus-visible:ring-ring"
                >
                  <tipo.icon aria-hidden="true" className="size-4 shrink-0 text-muted-foreground" />
                  <span className="min-w-0 flex-1 sm:flex sm:items-baseline sm:gap-3">
                    <span className="block truncate font-medium">{item.nome}</span>
                    <span className="block text-muted-foreground">
                      {tipo.label}
                      {item.ativo ? null : ' · Inativo'}
                    </span>
                  </span>
                  <time
                    dateTime={item.criadoEm}
                    title={formatDateTime(item.criadoEm)}
                    className="shrink-0 text-muted-foreground"
                  >
                    {formatRelative(item.criadoEm)}
                  </time>
                </Link>
              </li>
            )
          })}
        </ul>
      )}
    </InicioSection>
  )
}
