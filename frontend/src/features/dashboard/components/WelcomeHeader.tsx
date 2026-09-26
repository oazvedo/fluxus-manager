function greeting(hour: number) {
  if (hour < 12) return 'Bom dia'
  if (hour < 18) return 'Boa tarde'
  return 'Boa noite'
}

export function WelcomeHeader() {
  const now = new Date()
  const date = new Intl.DateTimeFormat('pt-BR', { weekday: 'long', day: 'numeric', month: 'long' }).format(now)
  const today = date.charAt(0).toUpperCase() + date.slice(1) // "Sábado, 26 de setembro"

  return (
    <div className="space-y-1">
      <p className="text-sm text-muted-foreground">{today}</p>
      <h1 className="text-2xl font-semibold tracking-tight md:text-3xl">{greeting(now.getHours())}</h1>
      <p className="text-sm text-muted-foreground">
        Bem-vindo ao FluxusManager. Aqui você acompanha e gerencia suas empresas em um só lugar.
      </p>
    </div>
  )
}
