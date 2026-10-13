#!/usr/bin/env bash
# =============================================================================
# Smoke test pós-deploy. Uso: ./scripts/smoke-test.sh http://<host>:<porta> [versao-esperada]
#   1) espera o /health responder 200 (com retry)
#   2) confere a versão publicada (se informada)
#   3) faz login (JWT) e chama um endpoint protegido
# =============================================================================
set -euo pipefail

BASE_URL="${1:?informe a URL base, ex.: http://localhost:8080}"
EXPECTED_VERSION="${2:-}"
TRIES="${SMOKE_TRIES:-30}"
WAIT="${SMOKE_WAIT:-10}"

echo ">> aguardando ${BASE_URL}/health"
for i in $(seq 1 "$TRIES"); do
  if HEALTH=$(curl -fsS --max-time 10 "${BASE_URL}/health" 2>/dev/null); then
    echo "   OK na tentativa $i: $HEALTH"
    break
  fi
  if [[ "$i" == "$TRIES" ]]; then
    echo "!! /health não respondeu após $TRIES tentativas"; exit 1
  fi
  sleep "$WAIT"
done

if [[ -n "$EXPECTED_VERSION" ]]; then
  echo "$HEALTH" | grep -q "\"version\":\"${EXPECTED_VERSION}\"" \
    || { echo "!! versão publicada diferente da esperada (${EXPECTED_VERSION})"; exit 1; }
  echo "   versão ${EXPECTED_VERSION} confirmada"
fi

echo ">> login com usuário de demonstração"
LOGIN=$(curl -fsS --max-time 15 -X POST "${BASE_URL}/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@kaizen.com.br","senha":"kaizen123"}')
TOKEN=$(echo "$LOGIN" | sed -n 's/.*"token":"\([^"]*\)".*/\1/p')
[[ -n "$TOKEN" ]] || { echo "!! não veio token no login"; exit 1; }
echo "   token JWT recebido"

echo ">> GET /api/empresas (protegido)"
CODE=$(curl -s -o /tmp/empresas.json -w "%{http_code}" --max-time 15 \
  -H "Authorization: Bearer ${TOKEN}" "${BASE_URL}/api/empresas?page=1&pageSize=5")
[[ "$CODE" == "200" ]] || { echo "!! esperado 200, veio $CODE"; cat /tmp/empresas.json; exit 1; }
echo "   HTTP 200: $(head -c 200 /tmp/empresas.json)..."

echo ">> smoke test OK em ${BASE_URL}"
