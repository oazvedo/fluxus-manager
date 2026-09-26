#!/usr/bin/env bash
# Inicia o trabalho numa issue: cria a branch vinculada à issue (a partir da dev),
# faz checkout, atribui a issue a você e move o card do Project para "In Progress".
#
# Uso: scripts/start-issue.sh <issue> <descricao> [tipo]
#   ex.: scripts/start-issue.sh 1 multi-tenant           → feat/1-multi-tenant
#        scripts/start-issue.sh 42 token-expirado bugfix → bugfix/42-token-expirado
set -euo pipefail

PROJECT_OWNER="${PROJECT_OWNER:-oazvedo}"
PROJECT_NUMBER="${PROJECT_NUMBER:-5}"

issue="${1:?informe o número da issue}"
slug="${2:?informe a descrição curta (ex.: multi-tenant)}"
type="${3:-feat}"
branch="${type}/${issue}-${slug}"

source "$(git rev-parse --show-toplevel)/.githooks/lib/conventions.sh"
validate_branch "$branch"

git fetch -q origin dev
gh issue develop "$issue" --base dev --name "$branch" --checkout
gh issue edit "$issue" --add-assignee @me >/dev/null

issue_url=$(gh issue view "$issue" --json url -q .url)
project_id=$(gh project view "$PROJECT_NUMBER" --owner "$PROJECT_OWNER" --format json -q .id)
item_id=$(gh project item-add "$PROJECT_NUMBER" --owner "$PROJECT_OWNER" --url "$issue_url" --format json -q .id)
status='.fields[] | select(.name=="Status")'
field_id=$(gh project field-list "$PROJECT_NUMBER" --owner "$PROJECT_OWNER" --format json -q "$status | .id")
option_id=$(gh project field-list "$PROJECT_NUMBER" --owner "$PROJECT_OWNER" --format json \
  -q "$status | .options[] | select(.name==\"In Progress\") | .id")
gh project item-edit --id "$item_id" --project-id "$project_id" --field-id "$field_id" --single-select-option-id "$option_id" >/dev/null

echo "✓ Branch $branch vinculada à issue #$issue; card em In Progress."
