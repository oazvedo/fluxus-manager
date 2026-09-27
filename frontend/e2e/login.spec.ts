import { expect, test } from '@playwright/test'

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

test('login responsivo e sem sessão persistida após recarregar', async ({ page }, testInfo) => {
  await page.setViewportSize({ width: 375, height: 740 })
  await page.goto('/login')
  await expect(page.getByRole('heading', { name: 'Entrar na sua conta' })).toBeVisible()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
  await page.screenshot({ path: testInfo.outputPath('login-mobile.png') })
  await page.getByLabel('E-mail', { exact: true }).fill('admin@fluxus.local')
  await page.getByLabel('Senha', { exact: true }).fill('senha-exclusiva-e2e-59')
  await page.getByRole('button', { name: 'Entrar', exact: true }).click()
  await expect(page).toHaveURL('/')
  await page.reload()
  await expect(page).toHaveURL('/login')
  expect(await page.evaluate(() => Object.keys(localStorage).length + Object.keys(sessionStorage).length)).toBe(0)
})
