# Estes valores viram variáveis do GitHub Environment (ver infra/scripts/sync-github-vars.sh).

output "app_url" {
  description = "URL pública (frontend em /, API em /api)."
  value       = "https://${aws_cloudfront_distribution.main.domain_name}"
}

output "aws_region" {
  value = data.aws_region.current.region
}

output "ecr_repository_url" {
  value = aws_ecr_repository.api.repository_url
}

output "ec2_instance_id" {
  value = aws_instance.api.id
}

output "frontend_bucket" {
  value = aws_s3_bucket.frontend.bucket
}

output "artifacts_bucket" {
  value = aws_s3_bucket.artifacts.bucket
}

output "cloudfront_distribution_id" {
  value = aws_cloudfront_distribution.main.id
}
