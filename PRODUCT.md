# Product

## Register

product

## Users

Equipe interna (administradores) que cadastra e mantém as empresas clientes e os acessos de quem usa o sistema.
Poucas pessoas, uso frequente ao longo do dia, em desktop, no escritório. Estão no meio de uma tarefa (cadastrar,
corrigir um dado, inativar um acesso) e querem terminar rápido, sem perder o lugar na lista.

## Product Purpose

FluxusManager é um sistema de gestão empresarial multi-tenant. A área administrativa existe para manter a base
confiável: empresas com CNPJ válido e sem duplicidade, usuários com e-mail único, e tudo auditado. Sucesso é
cadastrar ou corrigir um registro em segundos, sem erro e sem sair da tela em que se estava.

## Brand Personality

Preciso, rápido, discreto. A interface some na tarefa: linguagem direta em português, sem entusiasmo artificial,
mensagens que dizem o que aconteceu e o que fazer. Referência de sensação: Linear (densidade, velocidade,
neutro com um acento, zero decoração).

## Anti-references

- Template SaaS genérico: cards de métricas, gradientes, ilustrações, cara de "admin template".
- ERP antigo e poluído: telas lotadas de botões, abas e campos, tabelas cinzentas sem hierarquia.

## Design Principles

1. **A tarefa vem primeiro.** Cada tela tem uma ação principal clara; o resto é secundário e silencioso.
2. **Manter o contexto.** Criar e editar acontecem sem perder a lista, a página ou o filtro atual.
3. **Erro evitado vale mais que erro explicado.** Validar e formatar enquanto o usuário digita; quando falhar, dizer como corrigir, no campo.
4. **Familiaridade é recurso.** Padrões conhecidos (tabela, painel lateral, menu de ações), um vocabulário visual só em todas as telas.
5. **Denso sem ser apertado.** Muita informação por tela, organizada por espaço e alinhamento, não por linhas e caixas.

## Accessibility & Inclusion

WCAG 2.2 AA. Todo fluxo completo pelo teclado, foco sempre visível, status nunca só por cor,
respeitar `prefers-reduced-motion`, funcionar com zoom de 200%.
