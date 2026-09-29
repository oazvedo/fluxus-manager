import { expect, test, type Page } from '@playwright/test'

// Interface do fluxo de solicitação e do painel da plataforma com a API simulada (page.route):
// não depende do banco nem dos endpoints do backend estarem publicados.

const token = 'C3'.repeat(32)

const resumo = {
  id: 's1', razaoSocial: 'Acme Comércio Ltda', nomeFantasia: 'Acme', cnpj: '11222333000181',
  responsavelNome: 'Ana Souza', responsavelEmail: 'ana@acme.com.br', status: 'PendenteAnalise',
  criadaEm: '2026-09-27T13:00:00Z', verificadaEm: '2026-09-27T13:10:00Z', decididaEm: null, emailPendente: false,
}
const detalhe = {
  ...resumo, responsavelTelefone: '11987654321', decididaPor: null, observacaoInterna: null, motivoRecusa: null, empresaId: null,
  historico: [{ evento: 'Criada', em: '2026-09-27T13:00:00Z', por: null }, { evento: 'EmailVerificado', em: '2026-09-27T13:10:00Z', por: null }],
  emails: [{ tipo: 'Verificacao', status: 'Enviado', tentativas: 1, ultimaTentativaEm: '2026-09-27T13:00:05Z' }],
}

async function entrarComo(page: Page, administradorPlataforma: boolean) {
  await page.addInitScript((admin) => sessionStorage.setItem('fluxus.session.v1', JSON.stringify({
    id: 'sessao-e2e', email: 'equipe@fluxus.local', accessToken: 'jwt', tokenType: 'Bearer', expiresIn: 3600,
    tenantId: 't1', role: 'Administrador', permissions: ['empresas.editar'], refreshToken: 'r',
    refreshTokenExpiresAt: '2099-01-01T00:00:00Z', administradorPlataforma: admin,
  })), administradorPlataforma)
}

test('solicita cadastro pelo login sem conta e recebe confirmação neutra', async ({ page }) => {
  let corpo: Record<string, unknown> = {}
  await page.route('**/api/solicitacoes-cadastro', async (route) => {
    corpo = route.request().postDataJSON() as Record<string, unknown>
    await route.fulfill({ status: 202, json: { mensagem: 'ok' } })
  })

  await page.goto('/login')
  await page.getByRole('link', { name: 'Solicitar cadastro da empresa' }).click()
  await expect(page).toHaveURL('/solicitar-cadastro')

  await page.getByRole('button', { name: 'Enviar solicitação' }).click()
  await expect(page.getByText('Informe a razão social.')).toBeVisible()

  await page.getByLabel('Razão social').fill('Acme Comércio Ltda')
  await page.getByLabel('CNPJ').fill('11222333000181')
  await expect(page.getByLabel('CNPJ')).toHaveValue('11.222.333/0001-81')
  await page.getByLabel('Seu nome').fill('Ana Souza')
  await page.getByLabel('E-mail').fill('ana@acme.com.br')
  await page.getByLabel(/Telefone/).fill('11987654321')
  await expect(page.getByLabel(/Telefone/)).toHaveValue('(11) 98765-4321')
  await page.getByRole('button', { name: 'Enviar solicitação' }).click()

  await expect(page.getByRole('heading', { name: 'Confirme seu e-mail' })).toBeVisible()
  await expect(page.getByRole('status')).toContainText('Se os dados estiverem corretos')
  expect(corpo).toEqual({
    razaoSocial: 'Acme Comércio Ltda', nomeFantasia: null, cnpj: '11222333000181',
    responsavelNome: 'Ana Souza', responsavelEmail: 'ana@acme.com.br', responsavelTelefone: '11987654321',
  })
})

test('confirma o e-mail com um clique e tira o token da URL', async ({ page }) => {
  let chamadas = 0
  await page.route('**/api/solicitacoes-cadastro/verificar', async (route) => {
    chamadas++
    await route.fulfill({ json: { status: 'PendenteAnalise' } })
  })
  await page.goto(`/solicitar-cadastro/verificar?token=${token}`)
  expect(chamadas).toBe(0)
  await page.getByRole('button', { name: 'Confirmar e-mail' }).click()
  await expect(page.getByRole('heading', { name: 'E-mail confirmado' })).toBeVisible()
  expect(chamadas).toBe(1)
  expect(page.url()).not.toContain(token)
})

test('link de confirmação vencido oferece pedir outro', async ({ page }) => {
  await page.route('**/api/solicitacoes-cadastro/verificar', (route) => route.fulfill({ status: 410, json: { status: 410 } }))
  await page.route('**/api/solicitacoes-cadastro/reenviar-verificacao', (route) => route.fulfill({ status: 202, json: {} }))
  await page.goto(`/solicitar-cadastro/verificar?token=${token}`)
  await page.getByRole('button', { name: 'Confirmar e-mail' }).click()
  await expect(page.getByRole('heading', { name: 'Link expirado' })).toBeVisible()
  await page.getByLabel('E-mail do responsável').fill('ana@acme.com.br')
  await page.getByRole('button', { name: 'Enviar novo link' }).click()
  await expect(page.getByRole('status')).toContainText('ana@acme.com.br')
})

test('acompanhamento mostra a recusa com o motivo', async ({ page }) => {
  await page.route('**/api/solicitacoes-cadastro/acompanhar', (route) => route.fulfill({ json: {
    status: 'Recusada', razaoSocial: 'Acme Comércio Ltda', criadaEm: '2026-09-27T13:00:00Z',
    verificadaEm: '2026-09-27T13:10:00Z', decididaEm: '2026-09-28T09:00:00Z', motivoRecusa: 'O CNPJ está baixado na Receita Federal.',
  } }))
  await page.goto(`/solicitar-cadastro/acompanhar?token=${token}`)
  await expect(page.getByText('Solicitação recusada')).toBeVisible()
  await expect(page.getByText('O CNPJ está baixado na Receita Federal.')).toBeVisible()
  await expect(page.getByRole('list', { name: 'Etapas da solicitação' }).getByRole('listitem')).toHaveCount(4)
})

test('sem papel de plataforma não vê o menu nem a fila, mesmo com permissões da empresa', async ({ page }) => {
  let chamadasFila = 0
  // A última rota registrada vence: a da plataforma fica por cima da genérica.
  // Só o /api da origem: um glob "**/api/**" pegaria também os módulos do Vite em /src/core/api/.
  await page.route(/^https?:\/\/[^/]+\/api\//, (route) => route.fulfill({ json: { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0, hasNextPage: false, hasPreviousPage: false } }))
  await page.route('**/api/plataforma/**', (route) => { chamadasFila++; return route.fulfill({ status: 403, json: { status: 403 } }) })
  await entrarComo(page, false)
  await page.goto('/plataforma/solicitacoes')
  await expect(page.getByRole('heading', { name: 'Esta área é da equipe Fluxus' })).toBeVisible()
  await expect(page.locator('[data-slot="sidebar"]').getByRole('link', { name: 'Solicitações de cadastro' })).toHaveCount(0)
  expect(chamadasFila).toBe(0)
})

test('administrador da plataforma recusa com motivo obrigatório', async ({ page }) => {
  let recusa: Record<string, unknown> | null = null
  await page.route('**/api/plataforma/solicitacoes-cadastro?*', (route) => route.fulfill({ json: {
    items: [resumo], page: 1, pageSize: 20, totalCount: 1, totalPages: 1, hasNextPage: false, hasPreviousPage: false,
  } }))
  await page.route('**/api/plataforma/solicitacoes-cadastro/s1', (route) => route.fulfill({ json: detalhe }))
  await page.route('**/api/plataforma/solicitacoes-cadastro/s1/recusar', async (route) => {
    recusa = route.request().postDataJSON() as Record<string, unknown>
    await route.fulfill({ json: { ...detalhe, status: 'Recusada', decididaEm: '2026-09-28T09:00:00Z', motivoRecusa: recusa.motivo } })
  })
  await entrarComo(page, true)
  await page.goto('/plataforma/solicitacoes')
  await page.getByRole('link', { name: 'Acme Comércio Ltda' }).click()
  await expect(page.getByRole('dialog').getByText('(11) 98765-4321')).toBeVisible()

  await page.getByRole('button', { name: 'Recusar', exact: true }).click()
  await page.getByRole('button', { name: 'Recusar solicitação' }).click()
  await expect(page.getByText(/pelo menos 10 caracteres/)).toBeVisible()
  expect(recusa).toBeNull()

  await page.getByLabel('Motivo para o solicitante').fill('O CNPJ está baixado na Receita Federal.')
  await page.getByRole('button', { name: 'Recusar solicitação' }).click()
  await expect(page.getByText('Acme Comércio Ltda recusada')).toBeVisible()
  expect(recusa).toEqual({ motivo: 'O CNPJ está baixado na Receita Federal.', observacaoInterna: null })
})
