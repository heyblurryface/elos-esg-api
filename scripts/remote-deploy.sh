#!/usr/bin/env bash
# =============================================================================
# Executado NA VM pelo pipeline, via SSH:
#   echo "$GHCR_TOKEN" | bash ~/elos-esg/<ambiente>/remote-deploy.sh <usuario-ghcr>
#
# Espera encontrar na mesma pasta:
#   docker-compose.yml   (copiado pelo pipeline)
#   .env                 (gerado pelo pipeline com os secrets do environment)
# O token do GHCR chega pela entrada padrão para não aparecer na linha de comando.
# =============================================================================
set -euo pipefail

cd "$(dirname "$0")"
[[ -f docker-compose.yml && -f .env ]] || { echo "!! faltando docker-compose.yml ou .env em $(pwd)"; exit 1; }
chmod 600 .env

set -a
# shellcheck disable=SC1091
source ./.env
set +a
PROJECT="${COMPOSE_PROJECT_NAME:?COMPOSE_PROJECT_NAME ausente no .env}"
GHCR_USER="${1:-github}"

if [[ ! -t 0 ]]; then
  GHCR_TOKEN="$(cat)"
  if [[ -n "$GHCR_TOKEN" ]]; then
    echo ">> login no GHCR como ${GHCR_USER}"
    echo "$GHCR_TOKEN" | docker login ghcr.io -u "$GHCR_USER" --password-stdin >/dev/null
  fi
fi

echo ">> [$PROJECT] baixando imagens: ${API_IMAGE}"
docker compose -p "$PROJECT" pull

echo ">> [$PROJECT] subindo containers e aguardando healthcheck (1o deploy do Oracle pode levar alguns minutos)"
if ! docker compose -p "$PROJECT" up -d --no-build --remove-orphans --wait --wait-timeout 900; then
  echo "!! containers não ficaram saudáveis. Últimos logs:"
  docker compose -p "$PROJECT" logs --tail 150
  exit 1
fi

docker compose -p "$PROJECT" ps

echo ">> limpando imagens antigas"
docker image prune -f >/dev/null || true
docker logout ghcr.io >/dev/null 2>&1 || true

echo ">> [$PROJECT] deploy concluído: versão ${APP_VERSION} (${ASPNETCORE_ENVIRONMENT}) na porta ${API_PORT}"
