/** Configuração vinda do ambiente (variáveis VITE_*). */
export const env = {
  /** Base da API. Em produção o nginx atende /api no mesmo domínio; em dev o Vite faz proxy. */
  apiUrl: import.meta.env.VITE_API_URL ?? '/api',
  mode: import.meta.env.MODE,
}
