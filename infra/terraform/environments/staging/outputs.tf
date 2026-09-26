output "app_url" {
  value = module.app.app_url
}

output "aws_region" {
  value = module.app.aws_region
}

output "ecr_repository_url" {
  value = module.app.ecr_repository_url
}

output "ec2_instance_id" {
  value = module.app.ec2_instance_id
}

output "frontend_bucket" {
  value = module.app.frontend_bucket
}

output "artifacts_bucket" {
  value = module.app.artifacts_bucket
}

output "cloudfront_distribution_id" {
  value = module.app.cloudfront_distribution_id
}
