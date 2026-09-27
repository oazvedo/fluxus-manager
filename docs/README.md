# Documentação do FluxusManager

Regras e padrões do projeto. Vale para quem escreve código aqui, pessoa ou ferramenta: antes de criar uma
entidade, uma tela ou um endpoint, leia o documento da área e siga o padrão que já existe.

| Documento | O que cobre |
| --- | --- |
| [Arquitetura](arquitetura.md) | Camadas, dependências, fluxo de uma requisição, multi-tenant, auditoria, exclusão lógica e erros |
| [Backend](backend.md) | Padrão de entidades, repositórios, services, DTOs, validators, controllers, EF Core, migrations, logs e testes |
| [Frontend](frontend.md) | Estrutura por feature, chamadas à API, React Query, formulários, rotas e o padrão das telas de cadastro |
| [Estilo e UX](estilo-e-ux.md) | Identidade visual, tokens, tipografia, componentes, estados, textos da interface e acessibilidade |
| [Fluxo de trabalho](fluxo-de-trabalho.md) | Branches, commits, PRs, CI, ambiente local e release |

O posicionamento do produto (público, personalidade, princípios de design) está no [`PRODUCT.md`](../PRODUCT.md).
Instalação, testes, auditoria e deploy estão no [`README`](../README.md) da raiz.

## Regras que valem para tudo

1. **Siga o padrão existente.** Empresas e Usuários são as referências, no backend e no frontend. Um cadastro novo
   repete a mesma estrutura, os mesmos nomes e o mesmo comportamento. Se algo precisar mudar, mude em todos.
2. **Português no domínio, inglês no técnico.** Entidades, casos de uso, rotas e mensagens em português
   (`Empresa`, `CriarAsync`, `/usuarios`, `useCriarEmpresa`). Peças genéricas de infraestrutura e componentes
   reutilizáveis em inglês (`RepositoryBase`, `GetByIdAsync`, `PageHeader`, `useHotkey`).
3. **Toda mensagem vista pelo usuário é em português do Brasil**, diz o que aconteceu e como resolver.
4. **Comentários explicam o porquê**, em português. O que o código faz deve ser legível sem comentário.
5. **A API é a fonte da verdade.** O frontend repete as validações para dar retorno rápido, mas nunca é a única barreira.
6. **Nada de segredo no código ou no log.** Senhas só como hash, nunca em resposta, log ou auditoria.
7. **Toda mudança passa pelo CI verde**: format, build, lint e testes. Validação local antes de abrir o PR.
