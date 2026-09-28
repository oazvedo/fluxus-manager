import { expect, test } from '@playwright/test'

const resetToken = 'A1'.repeat(32)

test('pede recuperação sem revelar se o e-mail está cadastrado', async ({ page }) => {
  let corpo: { email?: string } = {}
  await page.route('**/api/auth/forgot-password', async (route) => {
    corpo = route.request().postDataJSON() as { email?: string }
    await route.fulfill({ status: 200, json: { mensagem: 'Se o e-mail estiver cadastrado, você receberá um link para redefinir a senha.' } })
  })

  await page.goto('/login')
  await page.getByRole('link', { name: 'Esqueci minha senha' }).click()
  await expect(page).toHaveURL('/esqueci-senha')
  await page.getByLabel('E-mail', { exact: true }).fill('alguem@exemplo.com')
  await page.getByRole('button', { name: 'Enviar link' }).click()

  await expect(page.getByRole('status')).toContainText('Se o e-mail estiver cadastrado')
  expect(corpo.email).toBe('alguem@exemplo.com')
  await expect(page.getByRole('link', { name: 'Voltar para o login' })).toBeVisible()
})

test('redefine a senha com token no corpo e remove o token da URL ao concluir', async ({ page }) => {
  let corpo: { token?: string; novaSenha?: string } = {}
  let authorization: string | undefined
  await page.route('**/api/auth/reset-password', async (route) => {
    corpo = route.request().postDataJSON() as { token?: string; novaSenha?: string }
    authorization = route.request().headers().authorization
    await route.fulfill({ status: 204 })
  })

  await page.goto(`/redefinir-senha?token=${resetToken}`)
  await page.getByLabel('Nova senha', { exact: true }).fill('segredo123')
  await page.getByLabel('Repita a nova senha', { exact: true }).fill('segredo123')
  await page.getByRole('button', { name: 'Redefinir senha' }).click()

  await expect(page).toHaveURL('/login')
  await expect(page.getByRole('status')).toContainText('Senha redefinida')
  expect(corpo).toEqual({ token: resetToken, novaSenha: 'segredo123' })
  expect(authorization).toBeUndefined()
  expect(page.url()).not.toContain(resetToken)
})

test('mostra orientação para token já usado ou expirado', async ({ page }) => {
  await page.route('**/api/auth/reset-password', (route) => route.fulfill({
    status: 422, json: { status: 422, detail: 'O link de redefinição é inválido ou expirou. Solicite outro.' },
  }))
  await page.goto(`/redefinir-senha?token=${resetToken}`)
  await page.getByLabel('Nova senha', { exact: true }).fill('segredo123')
  await page.getByLabel('Repita a nova senha', { exact: true }).fill('segredo123')
  await page.getByRole('button', { name: 'Redefinir senha' }).click()

  await expect(page.getByRole('heading', { name: 'Link inválido ou expirado' })).toBeVisible()
  await expect(page.getByRole('link', { name: 'Solicitar outro link' })).toBeVisible()
})

test('login, cadastros autenticados, renovação e logout com API real', async ({ page }, testInfo) => {
  const errors: string[] = []
  page.on('pageerror', (error) => errors.push(error.message))
  let businessRequests = 0
  let refreshes = 0
  page.on('request', (request) => {
    if (/^\/api\/(empresas|usuarios|filiais)/.test(new URL(request.url()).pathname)) businessRequests++
    if (request.url().endsWith('/auth/refresh')) refreshes++
  })
  await page.goto('/empresas?pagina=1')
  await expect(page).toHaveURL(/\/login$/)
  expect(businessRequests).toBe(0)
  await page.screenshot({ path: testInfo.outputPath('login-desktop.png') })
  await page.getByRole('button', { name: 'Entrar', exact: true }).click()
  await expect(page.getByText('Informe sua senha.')).toBeVisible()
  await page.getByLabel('E-mail', { exact: true }).fill('admin@fluxus.local')
  await page.getByLabel('Senha', { exact: true }).fill('incorreta')
  await page.getByRole('button', { name: 'Entrar', exact: true }).click()
  await expect(page.getByRole('alert')).toContainText('E-mail ou senha inválidos')
  await page.getByLabel('Senha', { exact: true }).fill('senha-exclusiva-e2e-59')
  await page.getByRole('button', { name: 'Entrar', exact: true }).click()
  await expect(page).toHaveURL(/\/empresas\?pagina=1$/)
  await expect(page.getByRole('heading', { name: 'Empresas', exact: true })).toBeVisible()
  await expect(page.getByRole('link', { name: 'Fluxus Desenvolvimento Ltda' })).toBeVisible()

  // Simula a rejeição de um JWT expirado; a renovação seguinte chega à API real.
  await page.route('**/api/usuarios?*', async (route) => {
    await route.fulfill({ status: 401, json: { status: 401 } })
  }, { times: 1 })
  await page.getByRole('link', { name: 'Usuários', exact: true }).click()
  await expect(page.getByRole('link', { name: 'Administrador local', exact: true })).toBeVisible()
  expect(refreshes).toBe(1)
  await page.getByRole('link', { name: 'Filiais', exact: true }).click()
  await expect(page.getByRole('heading', { name: 'Filiais', exact: true })).toBeVisible()
  await expect(page.getByText('Nenhuma filial cadastrada')).toBeVisible()
  const logout = page.waitForResponse((response) => response.url().endsWith('/auth/logout') && response.status() === 204)
  await page.getByRole('button', { name: 'Sair da conta', exact: true }).click()
  await logout
  await expect(page).toHaveURL(/\/login$/)
  await page.goBack()
  await expect(page.getByRole('heading', { name: 'Entrar na sua conta' })).toBeVisible()
  expect(errors).toEqual([])
})

test('login responsivo preserva sessão após recarregar e logout remove persistência', async ({ page }, testInfo) => {
  await page.setViewportSize({ width: 375, height: 740 })
  await page.goto('/login')
  await expect(page.getByRole('heading', { name: 'Entrar na sua conta' })).toBeVisible()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
  await page.screenshot({ path: testInfo.outputPath('login-mobile.png') })
  await page.getByLabel('E-mail', { exact: true }).fill('admin@fluxus.local')
  await page.getByLabel('Senha', { exact: true }).fill('senha-exclusiva-e2e-59')
  await page.getByRole('button', { name: 'Entrar', exact: true }).click()
  await expect(page).toHaveURL('/')
  await page.goto('/empresas?pagina=1')
  await expect(page.getByRole('link', { name: 'Fluxus Desenvolvimento Ltda' })).toBeVisible()
  await page.reload()
  await expect(page).toHaveURL('/empresas?pagina=1')
  await expect(page.getByRole('link', { name: 'Fluxus Desenvolvimento Ltda' })).toBeVisible()

  // O JWT restaurado também pode expirar: a renovação usa o refresh persistido e grava o sucessor.
  let refreshes = 0
  page.on('request', (request) => { if (request.url().endsWith('/auth/refresh')) refreshes++ })
  await page.route('**/api/empresas?*', (route) => route.fulfill({ status: 401, json: { status: 401 } }), { times: 1 })
  await page.reload()
  await expect(page.getByRole('link', { name: 'Fluxus Desenvolvimento Ltda' })).toBeVisible()
  expect(refreshes).toBe(1)
  await page.reload()
  await expect(page.getByRole('link', { name: 'Fluxus Desenvolvimento Ltda' })).toBeVisible()
  expect(refreshes).toBe(1)

  await page.setViewportSize({ width: 1280, height: 800 })
  const logout = page.waitForResponse((response) => response.url().endsWith('/auth/logout') && response.status() === 204)
  await page.getByRole('button', { name: 'Sair da conta', exact: true }).click()
  await logout
  await page.reload()
  await expect(page.getByRole('heading', { name: 'Entrar na sua conta' })).toBeVisible()
  expect(await page.evaluate(() => Object.keys(localStorage).length + Object.keys(sessionStorage).length)).toBe(0)
})
