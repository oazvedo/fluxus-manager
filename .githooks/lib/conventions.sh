#!/usr/bin/env bash
# Regras de nomenclatura de branches e commits (usadas pelos hooks locais e pelo workflow de CI).

ISSUE_BRANCH_TYPES="feat|bugfix|hotfix"                   # exigem número da issue
FREE_BRANCH_TYPES="chore|docs|refactor|test|ci|release"   # número da issue opcional
BRANCH_TYPES="${ISSUE_BRANCH_TYPES}|${FREE_BRANCH_TYPES}"
COMMIT_TYPES="feat|fix|bugfix|hotfix|chore|docs|refactor|test|ci|perf|style|build|release|revert"

# main, dev, <feat|bugfix|hotfix>/<issue>-<descricao> ou <outro-tipo>/[<issue>-]<descricao>, tudo em minúsculas.
# Ex.: feat/1-multi-tenant, bugfix/42-token-expirado, chore/atualiza-pacotes, release/1.0.0
BRANCH_REGEX="^((main|dev)|(${ISSUE_BRANCH_TYPES})/[0-9]+-[a-z0-9._-]+|(${FREE_BRANCH_TYPES})/[a-z0-9._-]+)$"

# <tipo>(escopo opcional)!: descrição. Ex.: "feat(empresa): cadastro de empresas"
COMMIT_REGEX="^(${COMMIT_TYPES})(\([a-z0-9._-]+\))?!?: .+"

# Commits gerados pelo git/GitHub (merge e revert) são aceitos.
AUTO_COMMIT_REGEX="^(Merge |Revert \")"

# Branches que podem abrir PR para a main.
MAIN_SOURCE_REGEX="^(dev|release/.+|hotfix/.+)$"

validate_branch() {
  if [[ ! "$1" =~ $BRANCH_REGEX ]]; then
    echo "✗ Nome de branch inválido: '$1'"
    echo "  Use main, dev, <tipo>/<issue>-<descricao> (${ISSUE_BRANCH_TYPES//|/, })"
    echo "  ou <tipo>/<descricao> (${FREE_BRANCH_TYPES//|/, }), tudo em minúsculas."
    echo "  Ex.: feat/1-multi-tenant, bugfix/42-token-expirado, chore/atualiza-pacotes"
    return 1
  fi
}

# Número da issue contido no nome da branch (vazio se não houver). Ex.: feat/1-multi-tenant → 1
issue_from_branch() {
  if [[ "$1" =~ ^[a-z]+/([0-9]+)- ]]; then
    echo "${BASH_REMATCH[1]}"
  fi
}

is_auto_commit() {
  [[ "$(printf '%s\n' "$1" | head -n1)" =~ $AUTO_COMMIT_REGEX ]]
}

# $1 = mensagem, $2 = rótulo usado no erro (padrão: "Mensagem de commit")
validate_commit_msg() {
  local subject label="${2:-Mensagem de commit}"
  subject=$(printf '%s\n' "$1" | head -n1)
  is_auto_commit "$1" && return 0
  if [[ ! "$subject" =~ $COMMIT_REGEX ]]; then
    echo "✗ ${label} inválido(a): '$subject'"
    echo "  Use <tipo>(escopo opcional): descrição"
    echo "  Tipos: ${COMMIT_TYPES//|/, }"
    echo "  Ex.: feat(empresa): cadastro de empresas | fix: corrige expiração do token"
    return 1
  fi
}

# Exige que a mensagem referencie a issue da branch (ex.: "Refs #1"). $1 = mensagem, $2 = issue
validate_issue_ref() {
  [[ -z "$2" ]] && return 0
  is_auto_commit "$1" && return 0
  if [[ ! "$1" =~ \#$2([^0-9]|$) ]]; then
    echo "✗ O commit '$(printf '%s\n' "$1" | head -n1)' não referencia a issue #$2."
    echo "  Adicione 'Refs #$2' à mensagem (o hook prepare-commit-msg faz isso automaticamente)."
    return 1
  fi
}
