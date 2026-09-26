# ---------- Role da EC2 ----------

data "aws_iam_policy_document" "ec2_trust" {
  statement {
    actions = ["sts:AssumeRole"]

    principals {
      type        = "Service"
      identifiers = ["ec2.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "api" {
  name               = "${local.name}-api"
  assume_role_policy = data.aws_iam_policy_document.ec2_trust.json
}

# Acesso pelo Session Manager e execução de comandos de deploy via SSM (sem SSH).
resource "aws_iam_role_policy_attachment" "api_ssm" {
  role       = aws_iam_role.api.name
  policy_arn = "arn:aws:iam::aws:policy/AmazonSSMManagedInstanceCore"
}

resource "aws_iam_role_policy_attachment" "api_ecr" {
  role       = aws_iam_role.api.name
  policy_arn = "arn:aws:iam::aws:policy/AmazonEC2ContainerRegistryReadOnly"
}

data "aws_iam_policy_document" "api" {
  statement {
    sid       = "LerArtefatosDeDeploy"
    actions   = ["s3:GetObject", "s3:ListBucket"]
    resources = [aws_s3_bucket.artifacts.arn, "${aws_s3_bucket.artifacts.arn}/*"]
  }

  statement {
    sid       = "LerSegredosDoAmbiente"
    actions   = ["ssm:GetParameter", "ssm:GetParameters", "ssm:GetParametersByPath"]
    resources = ["arn:aws:ssm:${data.aws_region.current.region}:${local.account_id}:parameter/fluxus/${var.environment}/*"]
  }
}

resource "aws_iam_role_policy" "api" {
  name   = "deploy"
  role   = aws_iam_role.api.id
  policy = data.aws_iam_policy_document.api.json
}

resource "aws_iam_instance_profile" "api" {
  name = "${local.name}-api"
  role = aws_iam_role.api.name
}

# ---------- Role do Data Lifecycle Manager (snapshots) ----------

data "aws_iam_policy_document" "dlm_trust" {
  statement {
    actions = ["sts:AssumeRole"]

    principals {
      type        = "Service"
      identifiers = ["dlm.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "dlm" {
  name               = "${local.name}-dlm"
  assume_role_policy = data.aws_iam_policy_document.dlm_trust.json
}

resource "aws_iam_role_policy_attachment" "dlm" {
  role       = aws_iam_role.dlm.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSDataLifecycleManagerServiceRole"
}

# ---------- Permissões de deploy da role do GitHub Actions (criada no bootstrap) ----------

data "aws_iam_policy_document" "github_deploy" {
  statement {
    sid       = "EcrLogin"
    actions   = ["ecr:GetAuthorizationToken"]
    resources = ["*"]
  }

  statement {
    sid = "EcrPush"
    actions = [
      "ecr:BatchCheckLayerAvailability",
      "ecr:BatchGetImage",
      "ecr:CompleteLayerUpload",
      "ecr:DescribeImages",
      "ecr:InitiateLayerUpload",
      "ecr:PutImage",
      "ecr:UploadLayerPart",
    ]
    resources = [aws_ecr_repository.api.arn]
  }

  statement {
    sid     = "PublicarFrontendEArtefatos"
    actions = ["s3:ListBucket", "s3:GetObject", "s3:PutObject", "s3:DeleteObject"]
    resources = [
      aws_s3_bucket.frontend.arn, "${aws_s3_bucket.frontend.arn}/*",
      aws_s3_bucket.artifacts.arn, "${aws_s3_bucket.artifacts.arn}/*",
    ]
  }

  statement {
    sid       = "InvalidarCache"
    actions   = ["cloudfront:CreateInvalidation", "cloudfront:GetInvalidation"]
    resources = [aws_cloudfront_distribution.main.arn]
  }

  statement {
    sid     = "DeployNaEc2"
    actions = ["ssm:SendCommand"]
    resources = [
      aws_instance.api.arn,
      "arn:aws:ssm:${data.aws_region.current.region}::document/AWS-RunShellScript",
    ]
  }

  statement {
    sid       = "AcompanharDeploy"
    actions   = ["ssm:GetCommandInvocation", "ssm:ListCommandInvocations"]
    resources = ["*"]
  }
}

resource "aws_iam_role_policy" "github_deploy" {
  name   = "deploy-${var.environment}"
  role   = var.github_deploy_role_name
  policy = data.aws_iam_policy_document.github_deploy.json
}
