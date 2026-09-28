import { useEffect } from 'react'
import { problemMessage } from '@/core/api/problem'
import { NewRecordButton } from '@/shared/components/common/NewRecordButton'
import { PageHeader } from '@/shared/components/common/PageHeader'
import { TableMessage } from '@/shared/components/common/TableStates'
import { TablePagination } from '@/shared/components/common/TablePagination'
import { Button } from '@/shared/components/ui/button'
import { useHotkey } from '@/shared/hooks/use-hotkey'
import { useRecordParams } from '@/shared/hooks/use-record-params'
import { EmpresaSheet } from '../components/EmpresaSheet'
import { EMPRESAS_COLUMNS, EmpresasTable } from '../components/EmpresasTable'
import { useEmpresas } from '../hooks/use-empresas'

const count = new Intl.NumberFormat('pt-BR')

export function EmpresasPage() {
  const { page, setPage, editingId, creating, openCreate, closeSheet, editHref } = useRecordParams()
  const { data, error, isPending, isError, refetch, isFetching } = useEmpresas(page)
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
        title="Empresas"
        meta={data && !empty ? `${count.format(data.totalCount)} ${data.totalCount === 1 ? 'empresa' : 'empresas'}` : null}
        action={<NewRecordButton onClick={openCreate}>Nova empresa</NewRecordButton>}
      />

      <div className="border-y">
        <EmpresasTable
          empresas={data?.items}
          loading={isPending}
          editHref={editHref}
          message={
            isError && !data ? (
              <TableMessage
                tone="error"
                columns={EMPRESAS_COLUMNS}
                title="Não foi possível carregar as empresas"
                description={problemMessage(error)}
                action={
                  <Button variant="outline" onClick={() => refetch()} disabled={isFetching}>
                    Tentar de novo
                  </Button>
                }
              />
            ) : empty ? (
              <TableMessage
                columns={EMPRESAS_COLUMNS}
                title="Nenhuma empresa cadastrada"
                description="Cadastre as empresas clientes para depois incluir filiais e dar acesso às equipes."
                action={<Button onClick={openCreate}>Cadastrar empresa</Button>}
              />
            ) : null
          }
        />
      </div>

      {data ? <TablePagination result={data} onPageChange={setPage} /> : null}

      <EmpresaSheet creating={creating} editingId={editingId} onClose={closeSheet} />
    </div>
  )
}
