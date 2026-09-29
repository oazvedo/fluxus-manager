import { Button } from '@/shared/components/ui/button'
import { Input } from '@/shared/components/ui/input'
import { Label } from '@/shared/components/ui/label'
import { statusOrdem, statusRotulo } from '../lib/rotulos'
import type { FiltrosSolicitacoes } from '../types/solicitacao'

// Mesmo visual do Input: o select nativo já é acessível pelo teclado (igual ao formulário de convite).
const selectClassName = 'h-8 w-full min-w-0 rounded-lg border border-input bg-background px-2 py-1 text-base outline-none transition-colors focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 md:text-sm dark:bg-input/30'

/** Filtros da fila: status e período de recebimento. Cada troca vai para a URL e volta à primeira página. */
export function SolicitacoesFiltros({ filtros, filtrando, onChange, onClear }: {
  filtros: FiltrosSolicitacoes
  filtrando: boolean
  onChange: (nome: keyof FiltrosSolicitacoes, valor: string | null) => void
  onClear: () => void
}) {
  return (
    <div role="group" aria-label="Filtros" className="flex flex-wrap items-end gap-x-3 gap-y-3">
      <div className="w-full space-y-1.5 sm:w-48">
        <Label htmlFor="filtro-status" className="text-muted-foreground">Status</Label>
        <select id="filtro-status" className={selectClassName} value={filtros.status ?? ''}
          onChange={(event) => onChange('status', event.target.value || null)}>
          <option value="">Todos</option>
          {statusOrdem.map((status) => <option key={status} value={status}>{statusRotulo[status]}</option>)}
        </select>
      </div>
      <div className="space-y-1.5">
        <Label htmlFor="filtro-de" className="text-muted-foreground">Recebidas de</Label>
        <Input id="filtro-de" type="date" className="w-40 tabular-nums" value={filtros.de ?? ''} max={filtros.ate ?? undefined}
          onChange={(event) => onChange('de', event.target.value || null)} />
      </div>
      <div className="space-y-1.5">
        <Label htmlFor="filtro-ate" className="text-muted-foreground">até</Label>
        <Input id="filtro-ate" type="date" className="w-40 tabular-nums" value={filtros.ate ?? ''} min={filtros.de ?? undefined}
          onChange={(event) => onChange('ate', event.target.value || null)} />
      </div>
      {filtrando ? <Button variant="ghost" onClick={onClear}>Limpar filtros</Button> : null}
    </div>
  )
}
