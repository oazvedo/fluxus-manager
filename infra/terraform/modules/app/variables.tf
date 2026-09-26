variable "environment" {
  description = "Nome do ambiente (staging, production)."
  type        = string
}

variable "github_deploy_role_name" {
  description = "Role do GitHub Actions criada no bootstrap; recebe as permissões de deploy deste ambiente."
  type        = string
}

variable "instance_type" {
  description = "Tipo da EC2 (ARM/Graviton). Roda API + PostgreSQL em Docker."
  type        = string
  default     = "t4g.small"
}

variable "data_volume_size" {
  description = "Tamanho (GiB) do volume EBS com os dados do PostgreSQL."
  type        = number
  default     = 20
}

variable "snapshot_retention_days" {
  description = "Quantos snapshots diários do volume de dados manter."
  type        = number
  default     = 7
}

variable "vpc_cidr" {
  type    = string
  default = "10.20.0.0/16"
}

variable "cloudfront_price_class" {
  description = "PriceClass_All inclui a borda da América do Sul (menor latência no Brasil)."
  type        = string
  default     = "PriceClass_All"
}
