import { Button } from '@/shared/components/ui/button'
import { SheetClose } from '@/shared/components/ui/sheet'
import { Skeleton } from '@/shared/components/ui/skeleton'

/** Campos fantasmas enquanto o registro a editar carrega. Anunciado uma vez como status. */
export function SheetFormSkeleton({ label, fields = 3 }: { label: string; fields?: number }) {
  return (
    <div role="status" aria-label={label} className="space-y-6 px-4 py-2">
      {Array.from({ length: fields }, (_, item) => (
        <div key={item} aria-hidden="true" className="space-y-2">
          <Skeleton className="h-4 w-24" />
          <Skeleton className="h-8 w-full" />
        </div>
      ))}
    </div>
  )
}

/** Registro que não pôde ser aberto no painel: diz o motivo e devolve à lista. */
export function SheetLoadError({ message }: { message: string }) {
  return (
    <div className="space-y-4 px-4 py-2">
      <p role="alert" className="text-sm text-pretty text-muted-foreground">
        {message}
      </p>
      <SheetClose render={<Button variant="outline" />}>Voltar para a lista</SheetClose>
    </div>
  )
}
