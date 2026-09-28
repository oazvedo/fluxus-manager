/** Lê o token da URL sem persistir; o componente o descarta ao sair da rota. */
export function tokenDeRedefinicao(search: string): string | null {
  const token = new URLSearchParams(search).get('token')
  return token && /^[0-9A-F]{64}$/.test(token) ? token : null
}
