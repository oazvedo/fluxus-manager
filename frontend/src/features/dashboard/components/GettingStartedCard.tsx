import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card'

const steps = [
  { title: 'Cadastre sua empresa', description: 'Razão social, CNPJ e dados principais.' },
  { title: 'Adicione as filiais', description: 'Organize as unidades de cada empresa.' },
  { title: 'Convide sua equipe', description: 'Dê acesso com o perfil adequado a cada pessoa.' },
]

export function GettingStartedCard() {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Primeiros passos</CardTitle>
        <CardDescription>Para começar a usar o sistema.</CardDescription>
      </CardHeader>
      <CardContent>
        <ol className="space-y-4">
          {steps.map((step, index) => (
            <li key={step.title} className="flex gap-3">
              <span className="flex size-6 shrink-0 items-center justify-center rounded-full border text-xs font-medium text-muted-foreground">
                {index + 1}
              </span>
              <div className="space-y-0.5">
                <p className="text-sm font-medium leading-6">{step.title}</p>
                <p className="text-sm text-muted-foreground">{step.description}</p>
              </div>
            </li>
          ))}
        </ol>
      </CardContent>
    </Card>
  )
}
