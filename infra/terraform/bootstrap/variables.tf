variable "region" {
  description = "Região AWS principal."
  type        = string
  default     = "sa-east-1"
}

variable "github_repository" {
  description = "Repositório GitHub (owner/nome) autorizado a fazer deploy."
  type        = string
  default     = "oazvedo/fluxus-manager"
}

variable "github_environments" {
  description = "GitHub Environments que podem assumir a role de deploy."
  type        = list(string)
  default     = ["staging"]
}
