import { toast } from 'sonner'
import { problemMessage } from '@/core/api/problem'

/*
 * Único jeito de mostrar aviso no app. A duração depende do que o aviso pede da pessoa:
 * confirmação some sozinha; com "Desfazer" dura o bastante para alcançar pelo teclado (Alt+T);
 * erro fica até ser fechado, porque pode precisar ser lido com calma (WCAG 2.2.1).
 */
const DURACAO_CONFIRMACAO = 5_000
const DURACAO_COM_DESFAZER = 10_000

type SuccessOptions = {
  description?: string
  /** Mostra o botão "Desfazer" e estende a duração. */
  undo?: () => void
}

export const notify = {
  success(title: string, { description, undo }: SuccessOptions = {}) {
    return toast.success(title, {
      description,
      duration: undo ? DURACAO_COM_DESFAZER : DURACAO_CONFIRMACAO,
      action: undo ? { label: 'Desfazer', onClick: undo } : undefined,
    })
  },

  /** Aceita a mensagem pronta ou o erro da API (usa o `detail` do problema, ou a mensagem padrão). */
  error(error: unknown, { description }: { description?: string } = {}) {
    return toast.error(typeof error === 'string' ? error : problemMessage(error), {
      description,
      duration: Infinity,
    })
  },
}
