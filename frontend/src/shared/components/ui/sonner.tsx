import { Toaster as Sonner, type ToasterProps } from "sonner"
import { CircleAlertIcon, CircleCheckIcon, InfoIcon, Loader2Icon, TriangleAlertIcon } from "lucide-react"
import { useTheme } from '@/core/theme/useTheme'

/** Área de avisos. O visual fica em index.css; durações por tipo ficam em `notify` (shared/lib/notify.ts). */
const Toaster = ({ theme, ...props }: ToasterProps) => {
  const appTheme = useTheme().theme
  return (
    <Sonner
      theme={theme ?? appTheme}
      className="toaster group"
      closeButton
      // Fallback para chamadas diretas a toast(): com "Desfazer", 4s (padrão do sonner) não bastam. Prefira `notify`.
      duration={10_000}
      gap={8}
      visibleToasts={4}
      containerAriaLabel="Avisos"
      toastOptions={{ closeButtonAriaLabel: "Fechar aviso" }}
      icons={{
        success: <CircleCheckIcon className="size-4 text-success" />,
        info: <InfoIcon className="size-4 text-primary" />,
        warning: <TriangleAlertIcon className="size-4 text-warning" />,
        error: <CircleAlertIcon className="size-4 text-destructive" />,
        loading: <Loader2Icon className="size-4 animate-spin text-muted-foreground motion-reduce:animate-none" />,
      }}
      style={
        {
          "--normal-bg": "var(--popover)",
          "--normal-text": "var(--popover-foreground)",
          "--normal-border": "var(--border)",
          "--border-radius": "var(--radius)",
        } as React.CSSProperties
      }
      {...props}
    />
  )
}

export { Toaster }
