#!/usr/bin/env bash
# =============================================================================
# Configura o repositório no GitHub para o pipeline (usa o GitHub CLI "gh",
# que já vem instalado no Azure Cloud Shell):
#   - variables do repo: VM_HOST, VM_USER
#   - secret do repo:    VM_SSH_KEY
#   - environments "staging" e "production" com seus secrets
#   - aprovação manual (required reviewer = você) no environment "production"
#
# Uso (depois do azure-setup.sh):
#   gh auth login        # uma vez
#   bash infra/github-setup.sh <usuario>/<repositorio>
# =============================================================================
set -euo pipefail

REPO="${1:?informe <usuario>/<repositorio>}"
VALUES="${VALUES_FILE:-$HOME/elos-deploy-values.env}"
[[ -f "$VALUES" ]] || { echo "!! $VALUES não encontrado. Rode antes: bash infra/azure-setup.sh"; exit 1; }
# shellcheck disable=SC1090
source "$VALUES"

gh auth status >/dev/null 2>&1 || { echo "!! rode 'gh auth login' primeiro"; exit 1; }

echo ">> Variables e secret do repositório"
gh variable set VM_HOST --repo "$REPO" --body "$VM_HOST"
gh variable set VM_USER --repo "$REPO" --body "$VM_USER"
gh secret set VM_SSH_KEY --repo "$REPO" < "$VM_SSH_KEY_FILE"

MY_ID="$(gh api user --jq .id)"

echo ">> Environment staging (deploy automático)"
gh api --method PUT "repos/$REPO/environments/staging" --silent

echo ">> Environment production (exige sua aprovação)"
gh api --method PUT "repos/$REPO/environments/production" --silent --input - <<JSON
{ "reviewers": [ { "type": "User", "id": $MY_ID } ] }
JSON

echo ">> Secrets de cada ambiente"
gh secret set ORACLE_PASSWORD --repo "$REPO" --env staging    --body "$STAGING_ORACLE_PASSWORD"
gh secret set APP_DB_PASSWORD --repo "$REPO" --env staging    --body "$STAGING_APP_DB_PASSWORD"
gh secret set JWT_SECRET      --repo "$REPO" --env staging    --body "$STAGING_JWT_SECRET"
gh secret set ORACLE_PASSWORD --repo "$REPO" --env production --body "$PRODUCTION_ORACLE_PASSWORD"
gh secret set APP_DB_PASSWORD --repo "$REPO" --env production --body "$PRODUCTION_APP_DB_PASSWORD"
gh secret set JWT_SECRET      --repo "$REPO" --env production --body "$PRODUCTION_JWT_SECRET"

echo
echo "Pronto! Agora rode o pipeline:"
echo "  gh workflow run ci-cd.yml --repo $REPO"
echo "  (ou no GitHub: Actions > CI/CD - Elos ESG API > Run workflow)"
