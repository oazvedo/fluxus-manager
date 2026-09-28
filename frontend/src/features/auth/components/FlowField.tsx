/*
 * Campo de faixas do painel de marca: as mesmas barras de ponta diagonal do símbolo, em raias horizontais,
 * como registros passando por um fluxo. Poucas faixas levam o acento e desenham uma diagonal suave,
 * o caminho de um registro atravessando as raias. Determinístico: o desenho é igual em todo carregamento.
 */
const LARGURA = 480
const ALTURA = 640
const RAIA = 28
const ESPESSURA = 5
const INCLINACAO = (ESPESSURA * 2.5) / 3.5 // mesmo ângulo das pontas do símbolo

type Faixa = { x: number; largura: number; destaque: boolean }

function gerarRaias(): Faixa[][] {
  let semente = 7
  const aleatorio = () => {
    semente = (semente * 16807) % 2147483647
    return semente / 2147483647
  }
  const raias: Faixa[][] = []
  const total = Math.ceil(ALTURA / RAIA)
  for (let i = 0; i < total; i++) {
    const faixas: Faixa[] = []
    // O registro destacado desce da esquerda para a direita, uma raia por vez.
    const xDestaque = 40 + i * 17
    let x = -40 + aleatorio() * 60
    while (x < LARGURA) {
      const largura = 24 + aleatorio() * 120
      const destaque = i % 3 === 1 && Math.abs(x - xDestaque) < 70
      faixas.push({ x, largura, destaque })
      x += largura + 18 + aleatorio() * 90
    }
    raias.push(faixas)
  }
  return raias
}

const RAIAS = gerarRaias()

function caminho({ x, largura }: Faixa, y: number) {
  return `M${x} ${y}H${x + largura}L${x + largura - INCLINACAO} ${y + ESPESSURA}H${x}Z`
}

export function FlowField({ className }: { className?: string }) {
  return (
    <svg
      viewBox={`0 0 ${LARGURA} ${ALTURA}`}
      preserveAspectRatio="xMidYMid slice"
      aria-hidden="true"
      focusable="false"
      className={className}
    >
      {Array.from({ length: Math.floor(LARGURA / 96) }, (_, i) => (
        <line key={i} x1={(i + 1) * 96} y1={0} x2={(i + 1) * 96} y2={ALTURA}
          stroke="var(--brand-line)" strokeWidth={1} strokeDasharray="2 6" />
      ))}
      {RAIAS.map((faixas, i) => (
        <g
          key={i}
          // Entrada única ao abrir a tela: as raias deslizam em sequência. Sem deslocamento com movimento reduzido.
          className="transition-[opacity,translate] duration-500 ease-[cubic-bezier(0.23,1,0.32,1)] starting:-translate-x-3 starting:opacity-0 motion-reduce:starting:translate-x-0"
          style={{ transitionDelay: `${i * 22}ms` }}
        >
          {faixas.map((faixa) => (
            <path
              key={faixa.x}
              d={caminho(faixa, i * RAIA + (RAIA - ESPESSURA) / 2)}
              fill={faixa.destaque ? 'var(--brand-accent)' : 'var(--brand-surface-foreground)'}
              fillOpacity={faixa.destaque ? 0.9 : 0.1}
            />
          ))}
        </g>
      ))}
    </svg>
  )
}
