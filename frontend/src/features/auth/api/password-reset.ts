import { http } from '@/core/api/http'

export async function solicitarRedefinicaoSenha(email: string) {
  await http.post('/auth/forgot-password', { email })
}

export async function redefinirSenha(token: string, novaSenha: string) {
  await http.post('/auth/reset-password', { token, novaSenha })
}
