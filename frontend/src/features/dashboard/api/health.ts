import axios from 'axios'
import { http } from '@/core/api/http'

export type ApiHealth = {
  online: boolean
  status: string
  latencyMs: number
  checkedAt: Date
}

/** Consulta o /api/health e mede o tempo de resposta. Nunca lança: API fora do ar vira online=false. */
export async function getApiHealth(): Promise<ApiHealth> {
  const start = performance.now()
  const result = (online: boolean, status: string): ApiHealth => ({
    online,
    status,
    latencyMs: Math.round(performance.now() - start),
    checkedAt: new Date(),
  })

  try {
    const { data } = await http.get<string>('/health', { responseType: 'text' })
    return result(data === 'Healthy', data)
  } catch (error) {
    const status = axios.isAxiosError(error) && typeof error.response?.data === 'string' ? error.response.data : ''
    return result(false, status || 'Indisponível')
  }
}
