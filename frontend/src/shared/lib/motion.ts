/**
 * Vocabulário de movimento da interface. Curto, sem rebote, interrompível (transição CSS, não keyframe)
 * e sem deslocamento quando o sistema pede menos movimento.
 */

/** Curva de gaveta (painéis e barra lateral): sai rápido e assenta devagar, como algo com massa. */
export const easeDrawer = 'cubic-bezier(0.32,0.72,0,1)'

/** Retorno imediato ao pressionar: reduz levemente na hora e volta ao soltar. */
export const pressable =
  'transition-[scale,background-color,color,opacity] duration-150 ease-out active:scale-[0.97] motion-reduce:active:scale-100'

/** Entrada de conteúdo que troca na mesma tela: surge de 4px abaixo; com movimento reduzido, só aparece. */
export const enterFromBelow =
  'transition-[opacity,translate] duration-200 ease-out starting:translate-y-1 starting:opacity-0 motion-reduce:starting:translate-y-0'

/** Troca de ícone por estado (ex.: mostrar/ocultar, tema): os dois ficam no DOM e trocam por escala, opacidade e desfoque. */
export const iconSwap =
  'transition-[scale,opacity,filter] duration-200 ease-[cubic-bezier(0.2,0,0,1)] motion-reduce:transition-opacity'

/** Estado oculto de um ícone em `iconSwap`. */
export const iconHidden = 'scale-25 opacity-0 blur-[4px]'

/** `pressable` para itens de menu da barra lateral: mantém as transições de tamanho do recolhimento. */
export const menuPress =
  'transition-[width,height,padding,scale,color] duration-150 ease-out active:scale-[0.98] motion-reduce:active:scale-100'
