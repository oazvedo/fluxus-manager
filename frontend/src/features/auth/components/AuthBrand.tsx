import { BrandLogo } from '@/shared/components/brand/BrandMark'
import { ThemeToggle } from '@/shared/components/common/ThemeToggle'

/** Topo da coluna do formulário: a marca (só quando o painel lateral está oculto) e a troca de tema. */
export function AuthBrand() {
  return <div className="flex items-center justify-between">
    <BrandLogo className="lg:invisible" />
    <ThemeToggle />
  </div>
}
