import { Plus } from 'lucide-react'
import { useEffect } from 'react'
import { problemMessage } from '@/core/api/problem'
import { PageHeader } from '@/shared/components/common/PageHeader'
import { TableMessage } from '@/shared/components/common/TableStates'
import { TablePagination } from '@/shared/components/common/TablePagination'
import { Button } from '@/shared/components/ui/button'
import { useHotkey } from '@/shared/hooks/use-hotkey'
import { useRecordParams } from '@/shared/hooks/use-record-params'
import { FilialSheet } from '../components/FilialSheet'
import { FILIAIS_COLUMNS, FiliaisTable } from '../components/FiliaisTable'
import { useFiliais } from '../hooks/use-filiais'

const count = new Intl.NumberFormat('pt-BR')
export function FiliaisPage() {
  const { page, setPage, editingId, creating, openCreate, closeSheet, editHref } = useRecordParams()
  const { data, error, isPending, isError, refetch, isFetching } = useFiliais(page)
  const open = creating || editingId !== null
  useHotkey('n', openCreate, !open)
  useEffect(() => { if (data && data.totalPages > 0 && page > data.totalPages) setPage(data.totalPages) }, [data, page, setPage])
  const empty = data?.totalCount === 0
  return <div className="mx-auto w-full max-w-6xl space-y-6"><PageHeader title="Filiais" meta={data && !empty ? `${count.format(data.totalCount)} ${data.totalCount === 1 ? 'filial' : 'filiais'}` : null} action={<Button onClick={openCreate} aria-keyshortcuts="n"><Plus aria-hidden="true" />Nova filial<kbd aria-hidden="true" className="ml-1 hidden rounded border border-primary-foreground/25 px-1 font-sans text-[0.7rem] leading-4 text-primary-foreground/70 md:inline">N</kbd></Button>} />
    <div className="border-y"><FiliaisTable filiais={data?.items} loading={isPending} editHref={editHref} message={isError && !data ? <TableMessage columns={FILIAIS_COLUMNS} title="Não foi possível carregar as filiais" description={problemMessage(error)} action={<Button variant="outline" onClick={() => refetch()} disabled={isFetching}>Tentar de novo</Button>} /> : empty ? <TableMessage columns={FILIAIS_COLUMNS} title="Nenhuma filial cadastrada" description="Cadastre as unidades vinculadas à empresa." action={<Button onClick={openCreate}>Cadastrar filial</Button>} /> : null} /></div>
    {data ? <TablePagination result={data} onPageChange={setPage} /> : null}<FilialSheet creating={creating} editingId={editingId} onClose={closeSheet} />
  </div>
}
