import { useQuery } from '@tanstack/react-query'
import { getApiHealth } from '../api/health'

export function useApiHealth() {
  return useQuery({
    queryKey: ['dashboard', 'api-health'],
    queryFn: getApiHealth,
    refetchInterval: 30_000,
  })
}
