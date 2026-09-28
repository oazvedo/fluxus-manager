import { Plus } from 'lucide-react'
import type { ReactNode } from 'react'
import { Button } from '@/shared/components/ui/button'

/** Ação principal das telas de cadastro, com o atalho "N" visível em telas largas. */
export function NewRecordButton({ onClick, children }: { onClick: () => void; children: ReactNode }) {
  return (
    <Button onClick={onClick} aria-keyshortcuts="n">
      <Plus aria-hidden="true" />
      {children}
      <kbd
        aria-hidden="true"
        className="ml-1 hidden rounded border border-primary-foreground/25 px-1 font-sans text-[0.7rem] leading-4 text-primary-foreground/70 md:inline"
      >
        N
      </kbd>
    </Button>
  )
}
