/** Token do link do e-mail (`?token=`): 64 caracteres hexadecimais maiúsculos, ou null se faltar ou vier corrompido. */
export function tokenDoLink(search: string) {
  const token = new URLSearchParams(search).get('token')?.trim() ?? ''
  return /^[0-9A-F]{64}$/.test(token) ? token : null
}
