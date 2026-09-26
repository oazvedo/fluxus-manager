terraform {
  required_version = ">= 1.10"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 6.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.9"
    }
  }

  # Configuração parcial: bucket/região vêm de backend.hcl (ver backend.hcl.example).
  backend "s3" {
    key          = "staging/terraform.tfstate"
    use_lockfile = true
    encrypt      = true
  }
}

provider "aws" {
  region = var.region

  default_tags {
    tags = {
      Project     = "fluxus-manager"
      Environment = "staging"
      ManagedBy   = "terraform"
    }
  }
}

module "app" {
  source = "../../modules/app"

  environment             = "staging"
  github_deploy_role_name = var.github_deploy_role_name
  instance_type           = "t4g.small"
}
