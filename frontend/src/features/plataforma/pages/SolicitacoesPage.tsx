import { useEffect } from 'react'
import { getProblem, problemMessage } from '@/core/api/problem'
import { PageHeader } from '@/shared/components/common/PageHeader'
import { TableMessage } from '@/shared/components/common/TableStates'
import { TablePagination } from '@/shared/components/common/TablePagination'
import { Button } from '@/shared/components/ui/button'
import { SemPermissao } from '../components/SemPermissao'
import { SolicitacaoSheet } from '../components/SolicitacaoSheet'
import { SolicitacoesFiltros } from '../components/SolicitacoesFiltros'
import { SOLICITACOES_COLUMNS, SolicitacoesTable } from '../components/SolicitacoesTable'
import { useSolicitacoes } from '../hooks/use-solicitacoes'
import { useSolicitacoesParams } from '../hooks/use-solicitacoes-params'

const count = new Intl.NumberFormat('pt-BR')

export function SolicitacoesPage() {
  const { page, setPage, filtros, filtrando, setFiltro, limparFiltros, abertaId, fechar, abrirHref } = useSolicitacoesParams()
  const { data, error, isPending, isError, refetch, isFetching } = useSolicitacoes(page, filtros)

  // Página além da última (ex.: link antigo ou fila que andou): volta para a última página existente.
  useEffect(() => {
    if (data && data.totalPages > 0 && page > data.totalPages) setPage(data.totalPages)
  }, [data, page, setPage])

  // Permissão revogada depois do login: a API decide, a tela explica.
  if (isError && getProblem(error)?.status === 403) return <SemPermissao />

  const vazio = data?.totalCount === 0

  return (
    <div className="mx-auto w-full max-w-6xl space-y-6">
      <PageHeader
        title="Solicitações de cadastro"
        meta={data && !vazio ? `${count.format(data.totalCount)} ${data.totalCount === 1 ? 'solicitação' : 'solicitações'}` : null}
        description="Empresas que pediram para usar o Fluxus. Aprovar cria a empresa e convida o responsável."
      />

      <SolicitacoesFiltros filtros={filtros} filtrando={filtrando} onChange={setFiltro} onClear={limparFiltros} />

      <div className="border-y">
        <SolicitacoesTable
          solicitacoes={data?.items}
          loading={isPending}
          abrirHref={abrirHref}
          message={
            isError && !data ? (
              <TableMessage tone="error" columns={SOLICITACOES_COLUMNS} title="Não foi possível carregar as solicitações"
                description={problemMessage(error)}
                action={<Button variant="outline" onClick={() => refetch()} disabled={isFetching}>Tentar de novo</Button>} />
            ) : vazio && filtrando ? (
              <TableMessage columns={SOLICITACOES_COLUMNS} title="Nenhuma solicitação com estes filtros"
                description="Mude o status ou o período, ou limpe os filtros para ver todas."
                action={<Button variant="outline" onClick={limparFiltros}>Limpar filtros</Button>} />
            ) : vazio ? (
              <TableMessage columns={SOLICITACOES_COLUMNS} title="Nenhuma solicitação recebida"
                description="Os pedidos feitos em “Solicitar cadastro da empresa”, na tela de login, aparecem aqui." />
            ) : null
          }
        />
      </div>

      {data ? <TablePagination result={data} onPageChange={setPage} /> : null}

      <SolicitacaoSheet id={abertaId} onClose={fechar} />
    </div>
  )
}
