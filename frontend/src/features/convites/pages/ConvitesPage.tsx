import { useEffect } from 'react'
import { problemMessage } from '@/core/api/problem'
import { NewRecordButton } from '@/shared/components/common/NewRecordButton'
import { PageHeader } from '@/shared/components/common/PageHeader'
import { TableMessage } from '@/shared/components/common/TableStates'
import { TablePagination } from '@/shared/components/common/TablePagination'
import { Button } from '@/shared/components/ui/button'
import { useHotkey } from '@/shared/hooks/use-hotkey'
import { useRecordParams } from '@/shared/hooks/use-record-params'
import { ConviteSheet } from '../components/ConviteSheet'
import { CONVITES_COLUMNS, ConvitesTable } from '../components/ConvitesTable'
import { useConvites } from '../hooks/use-convites'

const count = new Intl.NumberFormat('pt-BR')

export function ConvitesPage() {
  const { page, setPage, creating, openCreate, closeSheet } = useRecordParams()
  const { data, error, isPending, isError, refetch, isFetching } = useConvites(page)

  useHotkey('n', openCreate, !creating)

  // Página além da última (ex.: link antigo): volta para a última página existente.
  useEffect(() => {
    if (data && data.totalPages > 0 && page > data.totalPages) setPage(data.totalPages)
  }, [data, page, setPage])

  const empty = data?.totalCount === 0

  return (
    <div className="mx-auto w-full max-w-6xl space-y-6">
      <PageHeader
        title="Convites"
        meta={data && !empty ? `${count.format(data.totalCount)} ${data.totalCount === 1 ? 'convite' : 'convites'}` : null}
        description="Convide pessoas por e-mail com um perfil de acesso. Quem ainda não tem conta cria a senha ao aceitar."
        action={<NewRecordButton onClick={openCreate}>Novo convite</NewRecordButton>}
      />

      <div className="border-y">
        <ConvitesTable
          convites={data?.items}
          loading={isPending}
          message={
            isError && !data ? (
              <TableMessage
                tone="error"
                columns={CONVITES_COLUMNS}
                title="Não foi possível carregar os convites"
                description={problemMessage(error)}
                action={
                  <Button variant="outline" onClick={() => refetch()} disabled={isFetching}>
                    Tentar de novo
                  </Button>
                }
              />
            ) : empty ? (
              <TableMessage
                columns={CONVITES_COLUMNS}
                title="Nenhum convite enviado"
                description="Envie o primeiro convite. Você acompanha aqui quem aceitou e quais convites venceram."
                action={<Button onClick={openCreate}>Enviar convite</Button>}
              />
            ) : null
          }
        />
      </div>

      {data ? <TablePagination result={data} onPageChange={setPage} /> : null}

      <ConviteSheet open={creating} onClose={closeSheet} />
    </div>
  )
}
