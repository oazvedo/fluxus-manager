#!/usr/bin/env bash
# Copia os outputs do Terraform para o GitHub Environment "staging" e liga o deploy automático.
# Rodar depois do "terraform apply" do bootstrap e do staging.
#
# Uso: infra/scripts/sync-github-vars.sh
set -euo pipefail

REPO="${REPO:-oazvedo/fluxus-manager}"
ENVIRONMENT="staging"
TF_DIR="$(cd "$(dirname "$0")/../terraform" && pwd)"

out() { terraform -chdir="$TF_DIR/$1" output -raw "$2"; }

role_arn=$(out bootstrap github_deploy_role_arn)

# Environment "staging" só aceita deploys da branch dev.
gh api -X PUT "repos/$REPO/environments/$ENVIRONMENT" --input - >/dev/null <<'JSON'
{ "deployment_branch_policy": { "protected_branches": false, "custom_branch_policies": true } }
JSON
gh api "repos/$REPO/environments/$ENVIRONMENT/deployment-branch-policies" -q '.branch_policies[].name' | grep -qx dev \
  || gh api -X POST "repos/$REPO/environments/$ENVIRONMENT/deployment-branch-policies" -f name=dev -f type=branch >/dev/null

set_var() {
  gh variable set "$1" --repo "$REPO" --env "$ENVIRONMENT" --body "$2"
  echo "  $1=$2"
}

echo "Variáveis do environment $ENVIRONMENT:"
set_var AWS_ROLE_ARN "$role_arn"
set_var AWS_REGION "$(out environments/staging aws_region)"
set_var ECR_REPOSITORY_URL "$(out environments/staging ecr_repository_url)"
set_var EC2_INSTANCE_ID "$(out environments/staging ec2_instance_id)"
set_var FRONTEND_BUCKET "$(out environments/staging frontend_bucket)"
set_var ARTIFACTS_BUCKET "$(out environments/staging artifacts_bucket)"
set_var CLOUDFRONT_DISTRIBUTION_ID "$(out environments/staging cloudfront_distribution_id)"
set_var APP_URL "$(out environments/staging app_url)"

gh variable set DEPLOY_STAGING_ENABLED --repo "$REPO" --body true
echo "✓ Deploy de staging habilitado (DEPLOY_STAGING_ENABLED=true)."
