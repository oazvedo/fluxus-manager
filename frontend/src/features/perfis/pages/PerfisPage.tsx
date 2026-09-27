import { Plus } from 'lucide-react'
import { useEffect } from 'react'
import { problemMessage } from '@/core/api/problem'
import { PageHeader } from '@/shared/components/common/PageHeader'
import { TableMessage } from '@/shared/components/common/TableStates'
import { TablePagination } from '@/shared/components/common/TablePagination'
import { Button } from '@/shared/components/ui/button'
import { useHotkey } from '@/shared/hooks/use-hotkey'
import { useRecordParams } from '@/shared/hooks/use-record-params'
import { PerfilSheet } from '../components/PerfilSheet'
import { PERFIS_COLUMNS, PerfisTable } from '../components/PerfisTable'
import { usePerfis } from '../hooks/use-perfis'

const count = new Intl.NumberFormat('pt-BR')

export function PerfisPage() {
  const { page, setPage, editingId, creating, openCreate, closeSheet, editHref } = useRecordParams()
  const { data, error, isPending, isError, refetch, isFetching } = usePerfis(page)
  const sheetOpen = creating || editingId !== null
  useHotkey('n', openCreate, !sheetOpen)

  useEffect(() => {
    if (data && data.totalPages > 0 && page > data.totalPages) setPage(data.totalPages)
  }, [data, page, setPage])

  const empty = data?.totalCount === 0
  return <div className="mx-auto w-full max-w-6xl space-y-6">
    <PageHeader title="Perfis e permissões" meta={data && !empty ? `${count.format(data.totalCount)} ${data.totalCount === 1 ? 'perfil' : 'perfis'}` : null}
      action={<Button onClick={openCreate} aria-keyshortcuts="n"><Plus aria-hidden="true" />Novo perfil
        <kbd aria-hidden="true" className="ml-1 hidden rounded border border-primary-foreground/25 px-1 font-sans text-[0.7rem] leading-4 text-primary-foreground/70 md:inline">N</kbd>
      </Button>} />
    <p className="-mt-4 text-sm text-muted-foreground">Os perfis controlam o que cada pessoa pode consultar e alterar nesta empresa.</p>
    <div className="border-y">
      <PerfisTable perfis={data?.items} loading={isPending} editHref={editHref} message={
        isError && !data ? <TableMessage columns={PERFIS_COLUMNS} title="Não foi possível carregar os perfis" description={problemMessage(error)}
          action={<Button variant="outline" onClick={() => refetch()} disabled={isFetching}>Tentar de novo</Button>} /> :
          empty ? <TableMessage columns={PERFIS_COLUMNS} title="Nenhum perfil cadastrado" description="Crie perfis para definir o acesso das pessoas vinculadas a esta empresa."
            action={<Button onClick={openCreate}>Cadastrar perfil</Button>} /> : null
      } />
    </div>
    {data ? <TablePagination result={data} onPageChange={setPage} /> : null}
    <PerfilSheet creating={creating} editingId={editingId} onClose={closeSheet} />
  </div>
}
