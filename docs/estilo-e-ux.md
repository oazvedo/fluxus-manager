# Estilo e UX

A direção vem do [`PRODUCT.md`](../PRODUCT.md): equipe interna, uso diário em desktop, gente no meio de uma tarefa.
A sensação de referência é o **Linear**: denso, rápido, neutro com um acento e sem decoração.

## Princípios

1. **A tarefa vem primeiro.** Cada tela tem uma ação principal clara. O resto é secundário e silencioso.
2. **Manter o contexto.** Criar e editar acontecem num painel lateral, sem perder a lista, a página ou o filtro.
3. **Evitar o erro vale mais que explicar o erro.** Validar e formatar enquanto se digita. Quando falhar, dizer como corrigir, no campo.
4. **Familiaridade é recurso.** Tabela, painel lateral e menu de ações, com o mesmo vocabulário visual em todas as telas.
5. **Denso sem ser apertado.** Organizar por espaço e alinhamento, não por linhas e caixas.

**Evite:**
- **Cara de template SaaS:** cards de métricas, gradientes, ilustrações, sombras fortes, ícones decorativos grandes.
- **Cara de ERP antigo:** telas lotadas de botões, abas e campos, tabelas cinzentas sem hierarquia.

## Base visual

- **Tailwind 4** com os tokens do shadcn em `src/index.css` (tema `base-nova`, base `neutral`, cores em `oklch`).
- **Use sempre os tokens semânticos**: `bg-background`, `text-foreground`, `text-muted-foreground`, `bg-muted`,
  `border`, `bg-primary`, `text-destructive`, `ring`. Nada de hex, `rgb()` ou `gray-500` no componente.
  A exceção é o ponto de status ativo (`bg-emerald-600 dark:bg-emerald-400`, no `StatusBadge`).
- Cor nova é token novo em `index.css`, com versão clara e escura.
- Classes condicionais com `cn()` de `@/shared/lib/utils`.
- **Tema:** só o claro por enquanto (o aviso do sonner está fixo no tema claro). Os tokens de `.dark` existem:
  não quebre o tema escuro usando cor fixa.
- **Raio:** `--radius` de 0.625rem e a escala `rounded-sm` … `rounded-xl` derivada dele.

## Tipografia

- Fonte **Geist Variable** (`font-sans`), carregada pelo `@fontsource-variable/geist`.
- Título da página (`h1` no `PageHeader`): `text-xl font-semibold tracking-tight`.
- Texto padrão da interface: `text-sm`. Texto secundário: `text-sm text-muted-foreground`. Detalhe (nome fantasia
  sob a razão social): `text-xs text-muted-foreground`.
- Números, datas, CNPJ e contagens com `tabular-nums`, para as colunas não "dançarem".
- Parágrafos de descrição com `text-pretty`. Texto longo em célula de tabela com `truncate` e o valor completo no `title`.

## Layout

- Área de conteúdo: `mx-auto w-full max-w-6xl`, com `space-y-6` entre cabeçalho, tabela e paginação.
- Página: `PageHeader` (título + contagem à esquerda, ação principal à direita).
- Tabela sem caixa em volta: só `border-y` no contêiner e as linhas divisórias da própria tabela.
- Colunas com largura fixa para status, datas e ações; a coluna principal ocupa o resto.
- Painel lateral à direita, `sm:max-w-md`, com cabeçalho, corpo rolável e rodapé fixo com os botões alinhados à direita.

## Componentes

Use o que já existe antes de criar:

| Precisa de | Use |
| --- | --- |
| Cabeçalho de página | `PageHeader` (`shared/components/common`) |
| Ativo / inativo | `StatusBadge`: ponto + texto, nunca só a cor |
| Paginação | `TablePagination` ("1–20 de 57", some quando cabe numa página) |
| Carregando / vazio / erro em tabela | `TableSkeletonRows` e `TableMessage` |
| Botão de envio | `SubmitButton` |
| Botões, campos, menus, painel, tabela | `shared/components/ui` (shadcn) |
| Ícones | `lucide-react`, tamanho padrão do componente. Decorativo com `aria-hidden="true"` |
| Aviso após uma ação | `toast` do `sonner` (canto inferior direito) |

- **Botões:** uma ação primária por área (`default`). Secundárias em `outline` ou `ghost`. Ação destrutiva no menu
  com `variant="destructive"`.
- **Atalho** da ação principal: tecla `N`, mostrada num `<kbd>` dentro do botão (só em telas médias ou maiores) e
  anunciada com `aria-keyshortcuts`.
- **Menu de ações da linha:** botão `⋯` (`Ellipsis`), `ghost`, `size="icon-sm"`. O primeiro item é "Editar <entidade>";
  depois um separador e ativar ou inativar.

## Estados

Toda tela trata todos:

| Estado | Como aparece |
| --- | --- |
| Carregando | Skeleton com a mesma altura do conteúdo real (sem spinner no meio da tela) |
| Paginando | A página atual continua visível até a próxima chegar |
| Vazio | Título, uma frase explicando o que é o cadastro e o botão de cadastrar |
| Erro ao carregar | Título, a mensagem da API e "Tentar de novo" |
| Enviando | `SubmitButton` desabilitado, com indicador e o mesmo texto |
| Validação | Mensagem embaixo do campo, borda de erro e foco no primeiro campo inválido |
| Conflito (409) | Mensagem no campo que causou (CNPJ, e-mail) |
| Registro inexistente | No painel: "Esta empresa não existe mais…" e "Voltar para a lista" |
| Sucesso | Aviso curto; em ação reversível, com "Desfazer" |

## Textos da interface

- **Português do Brasil**, direto, sem entusiasmo artificial e sem ponto de exclamação.
- **Só a primeira letra maiúscula** em títulos e botões: "Nova empresa", "Salvar alterações".
- **Botões:** verbo + objeto — "Cadastrar empresa", "Salvar alterações", "Tentar de novo", "Voltar para a lista".
  "Cancelar" fecha sem salvar.
- **Avisos de sucesso:** o fato, no particípio — "Empresa cadastrada", "Alterações salvas", "Fluxus Ltda inativada".
  Quando há consequência, diga: "Não consegue mais entrar no sistema."
- **Erros:** o que fazer, não o que deu errado por dentro — "Informe um CNPJ válido, com 14 caracteres. Pode ser com
  ou sem pontuação." Nunca mostre código, exceção ou "Erro 500".
- **Concordância com a entidade:** empresa é feminino ("Ativa", "Inativa", "Nova empresa"); usuário é masculino
  ("Ativo", "Inativo", "Novo usuário").
- **Descrição do painel** diz o que o formulário faz ou o que vale saber: "A empresa já começa ativa.", "Nome e e-mail de acesso."
- **Campo imutável** explica por quê e o que fazer: "O CNPJ não pode ser alterado. Para outro CNPJ, cadastre uma nova empresa."
- **Placeholder** só como exemplo de formato ("00.000.000/0000-00", "nome@empresa.com.br"), nunca no lugar do rótulo.

## Acessibilidade

Meta: **WCAG 2.2 AA**.

- **Teclado:** todo fluxo completo sem mouse. Foco sempre visível. `Esc` fecha o painel. Atalhos de uma tecla são
  ignorados enquanto se digita num campo.
- **Status nunca só por cor:** sempre com texto.
- **Formulários:** rótulo ligado ao campo (`htmlFor`/`id`), `aria-invalid` e `aria-describedby` apontando para o erro ou a ajuda.
- **Botões só com ícone** têm `aria-label` específico: "Ações de Fluxus Ltda", "Mostrar senha", "Próxima página".
- **Carregamento:** `aria-busy` onde algo carrega (painel, botão de envio). Skeleton da tabela com `aria-hidden`.
- **Links de verdade** para navegar (`<Link>`), botões para agir. A linha clicável sempre tem um link equivalente dentro dela.
- **Movimento:** animações curtas com `ease-out`, e respeite `prefers-reduced-motion` (`motion-reduce:`).
- **Zoom de 200%** sem quebrar: tabelas rolam na horizontal e o cabeçalho quebra linha (`flex-wrap`).
