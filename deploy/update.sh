#!/usr/bin/env bash
# Atualiza o servidor com a última versão da branch de deploy (padrão: main) e recria os containers.
# Rodado pelo workflow "Deploy EC2" a cada release (merge dev → main), ou manualmente no servidor.
# Uso (na raiz do repositório, no servidor): deploy/update.sh
set -euo pipefail

BRANCH="${DEPLOY_BRANCH:-main}"
COMPOSE=(docker compose -f deploy/docker-compose.yml)

cd "$(dirname "$0")/.."

# O servidor espelha a branch remota (o deploy/.env fica fora do git e é preservado).
git fetch -q origin "$BRANCH"
git checkout -q "$BRANCH"
git reset -q --hard "origin/$BRANCH"
echo "Versão: $(git log -1 --format='%h %s')"

"${COMPOSE[@]}" up -d --build --remove-orphans
docker image prune -f >/dev/null

for _ in $(seq 1 30); do
  if curl -fsS http://localhost/api/health >/dev/null 2>&1; then
    "${COMPOSE[@]}" ps
    echo "✓ Deploy concluído"
    exit 0
  fi
  sleep 4
done

echo "✗ /api/health não respondeu. Últimos logs da API:"
"${COMPOSE[@]}" logs --tail 80 api
exit 1
