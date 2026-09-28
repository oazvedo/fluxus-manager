import { useEffect } from 'react'
import { problemMessage } from '@/core/api/problem'
import { NewRecordButton } from '@/shared/components/common/NewRecordButton'
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
  const sheetOpen = creating || editingId !== null

  useHotkey('n', openCreate, !sheetOpen)

  // Página além da última (ex.: link antigo): volta para a última página existente.
  useEffect(() => {
    if (data && data.totalPages > 0 && page > data.totalPages) setPage(data.totalPages)
  }, [data, page, setPage])

  const empty = data?.totalCount === 0

  return (
    <div className="mx-auto w-full max-w-6xl space-y-6">
      <PageHeader
        title="Filiais"
        meta={data && !empty ? `${count.format(data.totalCount)} ${data.totalCount === 1 ? 'filial' : 'filiais'}` : null}
        action={<NewRecordButton onClick={openCreate}>Nova filial</NewRecordButton>}
      />

      <div className="border-y">
        <FiliaisTable
          filiais={data?.items}
          loading={isPending}
          editHref={editHref}
          message={
            isError && !data ? (
              <TableMessage
                tone="error"
                columns={FILIAIS_COLUMNS}
                title="Não foi possível carregar as filiais"
                description={problemMessage(error)}
                action={
                  <Button variant="outline" onClick={() => refetch()} disabled={isFetching}>
                    Tentar de novo
                  </Button>
                }
              />
            ) : empty ? (
              <TableMessage
                columns={FILIAIS_COLUMNS}
                title="Nenhuma filial cadastrada"
                description="Cadastre as unidades desta empresa, cada uma com seu CNPJ e endereço."
                action={<Button onClick={openCreate}>Cadastrar filial</Button>}
              />
            ) : null
          }
        />
      </div>

      {data ? <TablePagination result={data} onPageChange={setPage} /> : null}

      <FilialSheet creating={creating} editingId={editingId} onClose={closeSheet} />
    </div>
  )
}
