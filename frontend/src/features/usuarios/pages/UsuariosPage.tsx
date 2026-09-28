import { useEffect } from 'react'
import { problemMessage } from '@/core/api/problem'
import { NewRecordButton } from '@/shared/components/common/NewRecordButton'
import { PageHeader } from '@/shared/components/common/PageHeader'
import { TableMessage } from '@/shared/components/common/TableStates'
import { TablePagination } from '@/shared/components/common/TablePagination'
import { Button } from '@/shared/components/ui/button'
import { useHotkey } from '@/shared/hooks/use-hotkey'
import { useRecordParams } from '@/shared/hooks/use-record-params'
import { UsuarioSheet } from '../components/UsuarioSheet'
import { USUARIOS_COLUMNS, UsuariosTable } from '../components/UsuariosTable'
import { useUsuarios } from '../hooks/use-usuarios'

const count = new Intl.NumberFormat('pt-BR')

export function UsuariosPage() {
  const { page, setPage, editingId, creating, openCreate, closeSheet, editHref } = useRecordParams()
  const { data, error, isPending, isError, refetch, isFetching } = useUsuarios(page)
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
        title="Usuários"
        meta={data && !empty ? `${count.format(data.totalCount)} ${data.totalCount === 1 ? 'usuário' : 'usuários'}` : null}
        action={<NewRecordButton onClick={openCreate}>Novo usuário</NewRecordButton>}
      />

      <div className="border-y">
        <UsuariosTable
          usuarios={data?.items}
          loading={isPending}
          editHref={editHref}
          message={
            isError && !data ? (
              <TableMessage
                tone="error"
                columns={USUARIOS_COLUMNS}
                title="Não foi possível carregar os usuários"
                description={problemMessage(error)}
                action={
                  <Button variant="outline" onClick={() => refetch()} disabled={isFetching}>
                    Tentar de novo
                  </Button>
                }
              />
            ) : empty ? (
              <TableMessage
                columns={USUARIOS_COLUMNS}
                title="Nenhum usuário cadastrado"
                description="Cadastre quem vai entrar no sistema. Cada pessoa usa o próprio e-mail e senha."
                action={<Button onClick={openCreate}>Cadastrar usuário</Button>}
              />
            ) : null
          }
        />
      </div>

      {data ? <TablePagination result={data} onPageChange={setPage} /> : null}

      <UsuarioSheet creating={creating} editingId={editingId} onClose={closeSheet} />
    </div>
  )
}
