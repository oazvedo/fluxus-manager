# Frontend

React 19 + TypeScript (Vite), React Router, TanStack Query, react-hook-form + zod, Tailwind 4 e shadcn/ui sobre
Base UI. Visual e textos em [Estilo e UX](estilo-e-ux.md). Os modelos de tudo abaixo são as features
`empresas` e `usuarios`.

## Estrutura

```
frontend/src/
├── app/                    Montagem da aplicação
│   ├── layout/             AppLayout (cabeçalho + sidebar), AppSidebar, navigation.ts
│   ├── providers/          AppProviders (QueryClient, Tooltip, Toaster)
│   └── router/             router.tsx: junta as rotas das features
├── core/                   Infraestrutura sem tela
│   ├── api/                http.ts (cliente axios único), problem.ts (erros da API)
│   └── config/             env.ts (variáveis VITE_*)
├── features/<feature>/     Uma pasta por módulo do sistema (ver abaixo)
└── shared/                 Reutilizável por qualquer feature
    ├── components/ui/      Componentes do shadcn (gerados pela CLI)
    ├── components/common/  Componentes da aplicação: PageHeader, StatusBadge, TablePagination, TableStates, SubmitButton...
    ├── hooks/              useHotkey, useRecordParams...
    ├── lib/                cn, format (datas)
    └── types/              PagedResult
```

### Uma feature

```
features/empresas/
├── api/empresas.ts          Chamadas HTTP (uma função por endpoint)
├── types/empresa.ts         Tipos da API: Empresa, CriarEmpresa, AtualizarEmpresa
├── schemas/empresa.ts       Schemas zod dos formulários
├── hooks/use-empresas.ts    Queries e mutations do React Query
├── lib/cnpj.ts              Regras só desta feature (opcional)
├── components/              EmpresasTable, EmpresaForm, EmpresaSheet, EmpresaActions
├── pages/EmpresasPage.tsx   A tela da rota
├── routes.tsx               Rotas da feature (com lazy)
└── index.ts                 API pública: só exporta as rotas
```

- **Uma feature não importa de outra.** O que for usado por duas vai para `shared/`.
- De fora, só se importa o `index.ts` da feature (hoje, só as rotas).
- Dentro da feature, imports relativos (`../hooks/use-empresas`). Fora dela, o alias `@/` (`@/shared/...`, `@/core/...`).

## Nomes de arquivos e símbolos

| O quê | Arquivo | Símbolo |
| --- | --- | --- |
| Componente | `PascalCase.tsx` (`EmpresaForm.tsx`) | `export function EmpresaForm` (export nomeado, sem `default`) |
| Hook | `use-kebab-case.ts` (`use-empresas.ts`) | `useEmpresas`, `useCriarEmpresa` |
| API, tipos, schemas | substantivo da feature em minúsculas (`empresas.ts`, `empresa.ts`) | `listarEmpresas`, `Empresa`, `empresaFormSchema` |
| Página | `<Feature>Page.tsx` | `EmpresasPage` |

- **Domínio em português** (`listarEmpresas`, `useAlterarStatusEmpresa`, `Usuario`), genérico em inglês
  (`PageHeader`, `useHotkey`, `formatDate`).
- Tipos com `type`, não `interface`. Props como `type <Componente>Props`.
- Comentários `/** ... */` em português nos componentes e hooks exportados quando há comportamento a explicar.

## Chamadas à API

```ts
// api/empresas.ts
export const PAGE_SIZE = 20

export async function listarEmpresas(page: number) {
  const { data } = await http.get<PagedResult<Empresa>>('/empresas', { params: { page, pageSize: PAGE_SIZE } })
  return data
}

export async function alterarStatusEmpresa(id: string, ativo: boolean) {
  await http.patch(`/empresas/${id}/${ativo ? 'ativar' : 'inativar'}`)
}
```

- **Sempre pelo `http` de `@/core/api/http`.** Nunca `axios` ou `fetch` direto: é nele que ficam a base `/api`, o
  timeout, o header `X-Frontend-Url` (auditoria) e, no futuro, o token.
- Uma função `async` por endpoint, nomeada pelo caso de uso, devolvendo `data` já tipado.
- Os tipos em `types/` espelham os DTOs da API em camelCase (`EmpresaResponse` → `Empresa`, `CriarEmpresaRequest` →
  `CriarEmpresa`). Datas chegam como `string` ISO.

## Dados do servidor (React Query)

```ts
export const empresasKeys = {
  all: ['empresas'] as const,
  list: (page: number) => ['empresas', 'list', page] as const,
  detail: (id: string) => ['empresas', 'detail', id] as const,
}
```

- Todo dado da API passa pelo React Query, nos hooks da feature. Componente não chama a API direto.
- Chaves no objeto `<feature>Keys` (`all`, `list`, `detail`).
- **Lista:** `placeholderData: keepPreviousData`, para a página atual continuar na tela enquanto a próxima carrega.
- **Detalhe:** `retry: false` (404 não se tenta de novo) e `placeholderData` vindo da linha da lista em cache, para
  o painel abrir já preenchido.
- **Mutations:** ao criar ou mudar status, `invalidateQueries({ queryKey: <feature>Keys.all })`. Ao atualizar,
  `setQueryData` no detalhe e invalida as listas.
- Padrões globais (`AppProviders`): `staleTime` de 30 s, `retry: 1` e sem refetch ao focar a janela.
- Estado de tela (página, painel aberto) fica na **URL**, não em store global (ver [Telas de cadastro](#telas-de-cadastro)).

## Formulários

```tsx
const { register, handleSubmit, setError, formState: { errors } } = useForm<EmpresaFormValues>({
  resolver: zodResolver(empresaFormSchema),
  defaultValues: { razaoSocial: empresa?.razaoSocial ?? '', ... },
})

async function onSubmit(values: EmpresaFormValues) {
  try {
    await criar.mutateAsync(...)
    toast.success('Empresa cadastrada')
    onSaved()
  } catch (error) {
    if (!applyProblemToForm(error, setError, fields, 'cnpj')) toast.error(problemMessage(error))
  }
}
```

- **react-hook-form + zod** (`zodResolver`). O schema fica em `schemas/` e repete **as mesmas regras e limites dos
  validators da API**. Se um mudar, mude o outro no mesmo PR.
- Mensagens do schema dizem como corrigir: `'Informe a razão social.'`, `'Use no máximo 150 caracteres.'`.
- `<form noValidate>`: a validação é a nossa, não a do navegador. Ela roda ao enviar e o foco vai para o primeiro campo inválido.
- **Erros da API:** `applyProblemToForm(error, setError, campos, campoDoConflito)` leva os erros 400 para os campos
  e o 409 para o campo que causa o conflito (CNPJ, e-mail). Se nada casar com um campo, `toast.error(problemMessage(error))`.
- Cadastro e edição usam **o mesmo componente de formulário**. O que muda: campo imutável aparece somente leitura
  com explicação (CNPJ), e a senha não aparece na edição.
- Campo com máscara (ex.: CNPJ) usa `Controller`: guarda o valor normalizado e exibe formatado.
- Estrutura de campo com os componentes `Field`:

```tsx
<Field data-invalid={!!errors.razaoSocial}>
  <FieldLabel htmlFor="empresa-razaoSocial">Razão social</FieldLabel>
  <Input id="empresa-razaoSocial" autoComplete="organization"
    aria-invalid={!!errors.razaoSocial} aria-describedby={errorId('razaoSocial')} {...register('razaoSocial')} />
  <FieldError id={errorId('razaoSocial')} errors={[errors.razaoSocial]} />
</Field>
```

  - `id` do campo: `<entidade>-<campo>`; do erro: `<entidade>-<campo>-erro`; da ajuda: `<entidade>-<campo>-ajuda`.
  - `aria-invalid` e `aria-describedby` em todo campo. `autoComplete` certo (`off`, `organization`, `new-password`).
  - Campo opcional com `(opcional)` no rótulo. Obrigatório não leva asterisco.
- Botão de envio é o `SubmitButton` (desabilita e mostra o indicador enquanto envia).

## Rotas

```tsx
// features/empresas/routes.tsx
export const empresasRoutes: RouteObject[] = [
  {
    path: 'empresas',
    lazy: async () => ({ Component: (await import('./pages/EmpresasPage')).EmpresasPage }),
    handle: { title: 'Empresas' },
  },
]
```

- Toda página é carregada com `lazy`.
- `handle.title` é o título mostrado no cabeçalho (breadcrumb).
- Rota nova: exporte no `index.ts` da feature, espalhe no `router.tsx` (tirando o `comingSoon` correspondente),
  confira o item em `app/layout/navigation.ts` e marque o card em `ModuleGrid` como `available: true`.
- Caminhos em português, minúsculas e no plural, iguais aos da API (`/empresas`, `/usuarios`).

## Telas de cadastro

Toda tela de cadastro segue o mesmo padrão (`EmpresasPage`, `UsuariosPage`):

- **Página:** `PageHeader` com título, contagem (`57 empresas`) e o botão principal `Nova <entidade>` com atalho `N`
  (`useHotkey('n', openCreate, !sheetOpen)`). Embaixo, a tabela e o `TablePagination`.
- **Estado na URL** com `useRecordParams`: `?pagina=2`, `?novo` e `?editar=<id>`. O link pode ser compartilhado,
  o voltar do navegador fecha o painel e a página se mantém. Página além da última volta para a última.
- **Tabela** (`<Feature>Table` + constante `<FEATURE>_COLUMNS`):
  - `table-fixed` com larguras fixas nas colunas secundárias e `min-w-2xl` (rola na horizontal em tela estreita).
  - Linhas `h-12`. Clique na linha abre a edição, exceto em links, botões e itens de menu.
  - O nome é um `<Link>` para `editHref(id)`, então funciona pelo teclado e com Ctrl/Cmd+clique.
  - Status com `StatusBadge`. Datas com `<time>`, `formatDate` e `title={formatDateTime(...)}`.
  - Última coluna: menu de ações `⋯` (`<Entidade>Actions`) com `aria-label="Ações de <nome>"`.
- **Estados:** `TableSkeletonRows` ao carregar; `TableMessage` para vazio (com a ação de cadastrar) e para erro
  (com "Tentar de novo"). No painel: skeleton, e "não existe mais" para 404.
- **Painel lateral** (`<Entidade>Sheet`, `Sheet` com `sm:max-w-md`): título `Nova <entidade>` ou `Editar <entidade>`,
  descrição curta, formulário e rodapé com `Cancelar` e o botão de envio. Fecha ao salvar.
- **Inativar** sem pedir confirmação: o aviso de sucesso traz **Desfazer**. Confirmação fica para ação que não dá
  para desfazer.

## Qualidade

```bash
cd frontend
npm run lint    # oxlint: zero avisos
npm run build   # tsc -b (checagem de tipos) + vite build
```

- O CI roda os dois. `noUnusedLocals` e `noUnusedParameters` estão ligados.
- `shared/components/ui/` vem do shadcn (`npx shadcn@latest add <componente>`) e fica fora do lint. Edite só o
  necessário (ex.: textos em português, `prefers-reduced-motion`) e registre a mudança no PR.
- Antes do PR, abra a tela com a API local e teste os estados: vazio, erro, validação, 409, cadastro, edição,
  teclado e console sem erro.

## Checklist: nova tela de cadastro

1. `types/`, `api/`, `schemas/` e `hooks/` da feature, espelhando a API.
2. `<Entidade>Form`, `<Entidade>Sheet`, `<Entidade>sTable` e `<Entidade>Actions` copiando o padrão de empresas/usuários.
3. `<Feature>Page` com `PageHeader`, atalho `N`, `useRecordParams`, estados e paginação.
4. `routes.tsx` + `index.ts`, registro no `router.tsx`, `navigation.ts` e `ModuleGrid`.
5. `npm run lint` e `npm run build` verdes, e teste no navegador.
