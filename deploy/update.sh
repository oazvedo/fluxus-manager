#!/usr/bin/env bash
# Atualiza o servidor com a versão mais recente da branch atual e recria os containers.
# Uso (na raiz do repositório, no servidor): deploy/update.sh
set -euo pipefail

cd "$(dirname "$0")/.."

git pull --ff-only
docker compose -f deploy/docker-compose.yml up -d --build --remove-orphans
docker image prune -f >/dev/null

docker compose -f deploy/docker-compose.yml ps
