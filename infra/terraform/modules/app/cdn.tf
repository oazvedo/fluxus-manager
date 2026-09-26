# ---------- CloudFront: um único domínio para frontend e API ----------
#   /api/*  → EC2 (sem cache)       demais rotas → S3 (frontend React)
# Mesmo domínio para os dois: sem CORS. Domínio próprio + ACM entram depois (aliases/viewer_certificate).

locals {
  # Políticas gerenciadas da AWS
  cache_policy_caching_disabled            = "4135ea2d-6df8-44a3-9df3-4b5a84be39ad"
  cache_policy_caching_optimized           = "658327ea-f89d-4fab-a63d-7e88639e58f6"
  origin_request_all_viewer_except_host    = "b689b0a8-53d0-40ab-baf2-68738e2966ac"
  response_headers_security_headers_policy = "67f7725c-6f97-4210-82d7-5512b31e9d03"
  frontend_origin_id                       = "frontend-s3"
  api_origin_id                            = "api-ec2"
}

resource "aws_cloudfront_origin_access_control" "frontend" {
  name                              = "${local.name}-frontend"
  origin_access_control_origin_type = "s3"
  signing_behavior                  = "always"
  signing_protocol                  = "sigv4"
}

resource "aws_cloudfront_function" "spa_rewrite" {
  name    = "${local.name}-spa-rewrite"
  runtime = "cloudfront-js-2.0"
  publish = true
  code    = file("${path.module}/spa-rewrite.js")
}

resource "aws_cloudfront_distribution" "main" {
  enabled             = true
  comment             = local.name
  default_root_object = "index.html"
  price_class         = var.cloudfront_price_class
  http_version        = "http2and3"

  origin {
    origin_id                = local.frontend_origin_id
    domain_name              = aws_s3_bucket.frontend.bucket_regional_domain_name
    origin_access_control_id = aws_cloudfront_origin_access_control.frontend.id
  }

  origin {
    origin_id   = local.api_origin_id
    domain_name = aws_eip.api.public_dns

    custom_origin_config {
      http_port              = 80
      https_port             = 443
      origin_protocol_policy = "http-only"
      origin_ssl_protocols   = ["TLSv1.2"]
    }
  }

  default_cache_behavior {
    target_origin_id           = local.frontend_origin_id
    viewer_protocol_policy     = "redirect-to-https"
    allowed_methods            = ["GET", "HEAD", "OPTIONS"]
    cached_methods             = ["GET", "HEAD"]
    compress                   = true
    cache_policy_id            = local.cache_policy_caching_optimized
    response_headers_policy_id = local.response_headers_security_headers_policy

    function_association {
      event_type   = "viewer-request"
      function_arn = aws_cloudfront_function.spa_rewrite.arn
    }
  }

  ordered_cache_behavior {
    path_pattern             = "/api/*"
    target_origin_id         = local.api_origin_id
    viewer_protocol_policy   = "redirect-to-https"
    allowed_methods          = ["GET", "HEAD", "OPTIONS", "PUT", "POST", "PATCH", "DELETE"]
    cached_methods           = ["GET", "HEAD"]
    compress                 = true
    cache_policy_id          = local.cache_policy_caching_disabled
    origin_request_policy_id = local.origin_request_all_viewer_except_host
  }

  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }

  viewer_certificate {
    cloudfront_default_certificate = true
  }
}
