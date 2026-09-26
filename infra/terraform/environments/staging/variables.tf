variable "region" {
  type    = string
  default = "sa-east-1"
}

variable "github_deploy_role_name" {
  description = "Output github_deploy_role_name do bootstrap."
  type        = string
  default     = "fluxus-manager-github-deploy"
}
