output "state_bucket" {
  description = "Bucket do state — use em environments/*/backend.hcl."
  value       = aws_s3_bucket.tfstate.bucket
}

output "github_deploy_role_name" {
  description = "Nome da role do GitHub Actions — use na variável github_deploy_role_name dos ambientes."
  value       = aws_iam_role.github_deploy.name
}

output "github_deploy_role_arn" {
  description = "ARN da role do GitHub Actions (variável AWS_ROLE_ARN do GitHub Environment)."
  value       = aws_iam_role.github_deploy.arn
}
