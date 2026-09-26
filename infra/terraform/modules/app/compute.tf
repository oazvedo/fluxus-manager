# ---------- EC2: API + PostgreSQL em Docker ----------

data "aws_ssm_parameter" "al2023_arm64" {
  name = "/aws/service/ami-amazon-linux-latest/al2023-ami-kernel-default-arm64"
}

resource "aws_instance" "api" {
  ami                    = data.aws_ssm_parameter.al2023_arm64.value
  instance_type          = var.instance_type
  subnet_id              = aws_subnet.public.id
  vpc_security_group_ids = [aws_security_group.api.id]
  iam_instance_profile   = aws_iam_instance_profile.api.name

  user_data = templatefile("${path.module}/user_data.sh.tftpl", {
    data_volume_id = aws_ebs_volume.data.id
  })

  metadata_options {
    http_tokens = "required" # IMDSv2
  }

  root_block_device {
    volume_type = "gp3"
    volume_size = 20
    encrypted   = true
  }

  tags = { Name = "${local.name}-api" }

  lifecycle {
    # Nova AMI publicada pela AWS não deve recriar a máquina; atualizações são feitas por deploy.
    ignore_changes = [ami, user_data]
  }
}

resource "aws_eip" "api" {
  domain   = "vpc"
  instance = aws_instance.api.id

  tags = { Name = "${local.name}-api" }
}

# ---------- Volume de dados do PostgreSQL + snapshots diários ----------

resource "aws_ebs_volume" "data" {
  availability_zone = aws_subnet.public.availability_zone
  size              = var.data_volume_size
  type              = "gp3"
  encrypted         = true

  tags = {
    Name     = "${local.name}-data"
    Snapshot = "${local.name}-data"
  }

  lifecycle {
    prevent_destroy = true
  }
}

resource "aws_volume_attachment" "data" {
  device_name = "/dev/sdf"
  volume_id   = aws_ebs_volume.data.id
  instance_id = aws_instance.api.id
}

resource "aws_dlm_lifecycle_policy" "data" {
  description        = "Snapshots diarios do volume de dados ${local.name}"
  execution_role_arn = aws_iam_role.dlm.arn
  state              = "ENABLED"

  policy_details {
    resource_types = ["VOLUME"]

    target_tags = {
      Snapshot = "${local.name}-data"
    }

    schedule {
      name = "diario"

      create_rule {
        interval      = 24
        interval_unit = "HOURS"
        times         = ["06:00"] # UTC = 03:00 em Brasília
      }

      retain_rule {
        count = var.snapshot_retention_days
      }

      copy_tags = true
    }
  }
}
