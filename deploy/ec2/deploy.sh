#!/bin/bash
# Executado na EC2 via SSM pelo workflow de deploy.
# Sobe a nova imagem da API; se o /api/health não responder, volta para a imagem anterior.
#
# Uso: deploy.sh <tag> <ambiente> <regiao> <ecr_repository_url>
set -euo pipefail

TAG="$1"
ENVIRONMENT="$2"
REGION="$3"
IMAGE="$4"

cd /opt/fluxus

previous=""
if [[ -f .env ]]; then
  previous=$(grep '^API_IMAGE_TAG=' .env | cut -d= -f2 || true)
fi

db_password=$(aws ssm get-parameter --region "$REGION" --name "/fluxus/$ENVIRONMENT/db-password" \
  --with-decryption --query Parameter.Value --output text)

write_env() {
  umask 077
  cat > .env <<EOF
API_IMAGE=$IMAGE
API_IMAGE_TAG=$1
ASPNETCORE_ENVIRONMENT=${ENVIRONMENT^}
POSTGRES_PASSWORD=$db_password
EOF
}

healthy() {
  for _ in $(seq 1 30); do
    curl -fsS http://localhost/api/health >/dev/null 2>&1 && return 0
    sleep 4
  done
  return 1
}

aws ecr get-login-password --region "$REGION" | docker login --username AWS --password-stdin "${IMAGE%%/*}"

echo "Deploy da imagem $TAG (anterior: ${previous:-nenhuma})"
write_env "$TAG"
docker compose pull api
docker compose up -d --remove-orphans

if healthy; then
  docker image prune -af --filter "until=168h" >/dev/null
  echo "✓ Deploy concluído: $TAG"
  exit 0
fi

echo "✗ /api/health não respondeu. Últimos logs da API:"
docker compose logs --tail 80 api

if [[ -n "$previous" && "$previous" != "$TAG" ]]; then
  echo "Voltando para a imagem anterior ($previous)..."
  write_env "$previous"
  docker compose up -d
  healthy && echo "✓ Rollback concluído: $previous" || echo "✗ Rollback também falhou"
fi

exit 1
