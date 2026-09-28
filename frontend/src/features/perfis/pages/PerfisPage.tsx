import { useEffect } from 'react'
import { problemMessage } from '@/core/api/problem'
import { NewRecordButton } from '@/shared/components/common/NewRecordButton'
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

  // Página além da última (ex.: link antigo): volta para a última página existente.
  useEffect(() => {
    if (data && data.totalPages > 0 && page > data.totalPages) setPage(data.totalPages)
  }, [data, page, setPage])

  const empty = data?.totalCount === 0

  return (
    <div className="mx-auto w-full max-w-6xl space-y-6">
      <PageHeader
        title="Perfis e permissões"
        meta={data && !empty ? `${count.format(data.totalCount)} ${data.totalCount === 1 ? 'perfil' : 'perfis'}` : null}
        description="Defina o que cada pessoa pode consultar e alterar nesta empresa."
        action={<NewRecordButton onClick={openCreate}>Novo perfil</NewRecordButton>}
      />

      <div className="border-y">
        <PerfisTable
          perfis={data?.items}
          loading={isPending}
          editHref={editHref}
          message={
            isError && !data ? (
              <TableMessage
                tone="error"
                columns={PERFIS_COLUMNS}
                title="Não foi possível carregar os perfis"
                description={problemMessage(error)}
                action={
                  <Button variant="outline" onClick={() => refetch()} disabled={isFetching}>
                    Tentar de novo
                  </Button>
                }
              />
            ) : empty ? (
              <TableMessage
                columns={PERFIS_COLUMNS}
                title="Nenhum perfil cadastrado"
                description="Crie um perfil e escolha as permissões. Depois, convide pessoas com ele."
                action={<Button onClick={openCreate}>Cadastrar perfil</Button>}
              />
            ) : null
          }
        />
      </div>

      {data ? <TablePagination result={data} onPageChange={setPage} /> : null}

      <PerfilSheet creating={creating} editingId={editingId} onClose={closeSheet} />
    </div>
  )
}
