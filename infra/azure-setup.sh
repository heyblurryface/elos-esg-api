#!/usr/bin/env bash
# =============================================================================
# Cria a infraestrutura na Azure para os ambientes staging e produção.
# Rodar no Azure Cloud Shell (Bash), dentro da pasta do repositório:
#   bash infra/azure-setup.sh
#
# O que cria:
#   - Resource group rg-elos-esg
#   - VM Ubuntu 22.04 com Docker + Docker Compose (via cloud-init) e swap de 4 GB
#   - Portas liberadas no NSG: 22 (SSH), 8081 (staging) e 8080 (produção)
#   - Chave SSH dedicada ao GitHub Actions (~/.ssh/elos_deploy)
#   - Arquivo ~/elos-deploy-values.env com os valores para o GitHub
#
# Variáveis opcionais: RG, LOCATION, VM_NAME, VM_SIZE, ADMIN_USER
#   ex.: LOCATION=eastus2 VM_SIZE=Standard_B2s bash infra/azure-setup.sh
# =============================================================================
set -euo pipefail

RG="${RG:-rg-elos-esg}"
LOCATION="${LOCATION:-eastus}"
VM_NAME="${VM_NAME:-vm-elos-esg}"
VM_SIZE="${VM_SIZE:-Standard_B2ms}"     # 2 vCPU / 8 GB (2 Oracle + 2 APIs com folga)
ADMIN_USER="${ADMIN_USER:-azureuser}"
KEY="$HOME/.ssh/elos_deploy"
OUT="$HOME/elos-deploy-values.env"

echo ">> Assinatura atual: $(az account show --query name -o tsv)"

# 1) Chave SSH exclusiva para o pipeline
if [[ ! -f "$KEY" ]]; then
  echo ">> Gerando chave SSH $KEY"
  ssh-keygen -t ed25519 -f "$KEY" -N "" -C "github-actions-elos-esg" >/dev/null
fi

# 2) cloud-init: instala Docker e cria swap
CLOUD_INIT="$(mktemp)"
cat > "$CLOUD_INIT" <<CLOUDINIT
#cloud-config
package_update: true
packages:
  - ca-certificates
  - curl
runcmd:
  - curl -fsSL https://get.docker.com | sh
  - usermod -aG docker ${ADMIN_USER}
  - systemctl enable --now docker
  - fallocate -l 4G /swapfile && chmod 600 /swapfile && mkswap /swapfile && swapon /swapfile
  - echo '/swapfile none swap sw 0 0' >> /etc/fstab
CLOUDINIT

# 3) Resource group + VM
echo ">> Criando resource group $RG em $LOCATION"
az group create -n "$RG" -l "$LOCATION" -o none

echo ">> Criando VM $VM_NAME ($VM_SIZE). Leva uns 2 minutos..."
az vm create \
  -g "$RG" -n "$VM_NAME" \
  --image Ubuntu2204 \
  --size "$VM_SIZE" \
  --admin-username "$ADMIN_USER" \
  --ssh-key-values "${KEY}.pub" \
  --custom-data "$CLOUD_INIT" \
  --public-ip-sku Standard \
  --os-disk-size-gb 64 \
  -o none

# 4) Portas da API (staging 8081, produção 8080)
echo ">> Liberando portas 8080 e 8081"
az vm open-port -g "$RG" -n "$VM_NAME" --port 8080 --priority 1010 -o none
az vm open-port -g "$RG" -n "$VM_NAME" --port 8081 --priority 1020 -o none

IP="$(az vm show -d -g "$RG" -n "$VM_NAME" --query publicIps -o tsv)"
echo ">> IP público: $IP"

# 5) Espera o cloud-init terminar e confere o Docker
echo ">> Aguardando instalação do Docker na VM (cloud-init)..."
for i in $(seq 1 30); do
  if ssh -i "$KEY" -o StrictHostKeyChecking=accept-new -o ConnectTimeout=10 "$ADMIN_USER@$IP" \
       "cloud-init status --wait >/dev/null 2>&1; docker --version && docker compose version" 2>/dev/null; then
    break
  fi
  [[ "$i" == "30" ]] && { echo "!! não consegui confirmar o Docker na VM"; exit 1; }
  sleep 10
done

# 6) Valores para o GitHub (senhas só com letras e números)
gen() { echo "Elos$(openssl rand -hex 12)"; }
cat > "$OUT" <<VALUES
VM_HOST=$IP
VM_USER=$ADMIN_USER
VM_SSH_KEY_FILE=$KEY
STAGING_ORACLE_PASSWORD=$(gen)
STAGING_APP_DB_PASSWORD=$(gen)
STAGING_JWT_SECRET=$(openssl rand -hex 32)
PRODUCTION_ORACLE_PASSWORD=$(gen)
PRODUCTION_APP_DB_PASSWORD=$(gen)
PRODUCTION_JWT_SECRET=$(openssl rand -hex 32)
VALUES
chmod 600 "$OUT"

cat <<MSG

==============================================================================
 Infra pronta!
   VM:        $VM_NAME ($VM_SIZE) em $LOCATION
   IP:        $IP
   Staging:   http://$IP:8081   (depois do 1o deploy)
   Produção:  http://$IP:8080   (depois do 1o deploy)

 Valores para o GitHub salvos em: $OUT
 Próximo passo (configura o GitHub automaticamente):
   bash infra/github-setup.sh <seu-usuario>/<seu-repositorio>

 Para economizar crédito depois da avaliação:
   az vm deallocate -g $RG -n $VM_NAME      (desliga, não cobra CPU)
   az group delete -n $RG --yes             (apaga tudo)
==============================================================================
MSG
