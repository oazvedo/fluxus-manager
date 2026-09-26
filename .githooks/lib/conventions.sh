#!/usr/bin/env bash
# Regras de nomenclatura de branches e commits (usadas pelos hooks locais e pelo workflow de CI).

BRANCH_TYPES="feat|bugfix|hotfix|chore|docs|refactor|test|ci|release"
COMMIT_TYPES="feat|fix|bugfix|hotfix|chore|docs|refactor|test|ci|perf|style|build|release|revert"

# main e dev, ou <tipo>/<descricao> em minúsculas. Ex.: feat/1-multi-tenant, release/1.0.0
BRANCH_REGEX="^((main|dev)|(${BRANCH_TYPES})/[a-z0-9._-]+)$"

# <tipo>(escopo opcional)!: descrição. Ex.: "feat(empresa): cadastro de empresas"
COMMIT_REGEX="^(${COMMIT_TYPES})(\([a-z0-9._-]+\))?!?: .+"

# Commits gerados pelo git/GitHub (merge e revert) são aceitos.
AUTO_COMMIT_REGEX="^(Merge |Revert \")"

# Branches que podem abrir PR para a main.
MAIN_SOURCE_REGEX="^(dev|release/.+|hotfix/.+)$"

validate_branch() {
  if [[ ! "$1" =~ $BRANCH_REGEX ]]; then
    echo "✗ Nome de branch inválido: '$1'"
    echo "  Use main, dev ou <tipo>/<descricao> em minúsculas."
    echo "  Tipos: ${BRANCH_TYPES//|/, }"
    echo "  Ex.: feat/1-multi-tenant, bugfix/login-token-expirado"
    return 1
  fi
}

validate_commit_msg() {
  local subject
  subject=$(printf '%s\n' "$1" | head -n1)
  if [[ "$subject" =~ $AUTO_COMMIT_REGEX ]]; then
    return 0
  fi
  if [[ ! "$subject" =~ $COMMIT_REGEX ]]; then
    echo "✗ Mensagem de commit inválida: '$subject'"
    echo "  Use <tipo>(escopo opcional): descrição"
    echo "  Tipos: ${COMMIT_TYPES//|/, }"
    echo "  Ex.: feat(empresa): cadastro de empresas | fix: corrige expiração do token"
    return 1
  fi
}
