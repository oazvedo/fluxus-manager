import { useMutation } from '@tanstack/react-query'
import { entrar } from '@/core/auth/actions'

export function useLogin() {
  return useMutation({ mutationFn: entrar, retry: false, gcTime: 0 })
}
