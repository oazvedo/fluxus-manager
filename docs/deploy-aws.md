# Deploy na AWS (staging)

```
Usuário ──HTTPS──► CloudFront (*.cloudfront.net)
                     ├── /       → S3 (frontend React)
                     └── /api/*  → EC2 t4g.small ─┬─ API (.NET, Docker)
                                                  └─ PostgreSQL 18 (Docker) → EBS /data (snapshot diário)
```

Depois da configuração inicial, **todo merge na `dev` com CI verde faz deploy automático** em staging
(workflow **Deploy staging**). Se o `/api/health` falhar, a EC2 volta sozinha para a imagem anterior.

Custo estimado: **~US$ 17/mês** (EC2 t4g.small ~12, EBS ~4, CloudFront/S3/ECR ~1).

---

## Configuração inicial (uma vez)

Todos os comandos são no Ubuntu (WSL), na raiz do projeto.

### 1. Conta e acesso

1. Crie a conta em https://aws.amazon.com (cartão de crédito necessário).
2. No console, ative o **IAM Identity Center** e crie um usuário para você com o permission set
   `AdministratorAccess` (evite usar o usuário root no dia a dia).
3. Instale a AWS CLI no WSL (sem sudo):
   ```bash
   curl -fsSL https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip -o /tmp/awscli.zip
   unzip -q /tmp/awscli.zip -d /tmp && /tmp/aws/install -i ~/.local/aws-cli -b ~/.local/bin
   aws configure sso          # siga o assistente; região: sa-east-1
   export AWS_PROFILE=<nome-do-perfil-criado>
   aws sts get-caller-identity # deve mostrar sua conta
   ```

### 2. Bootstrap (state do Terraform + acesso do GitHub)

```bash
cd infra/terraform/bootstrap
terraform init
terraform apply
```

Cria o bucket do state, o provedor OIDC do GitHub e a role de deploy.
O state deste passo fica local (`terraform.tfstate`, fora do git) — **guarde uma cópia** em lugar seguro.

### 3. Ambiente staging

```bash
cd ../environments/staging
cp backend.hcl.example backend.hcl   # troque <ACCOUNT_ID> pelo output state_bucket do bootstrap
terraform init -backend-config=backend.hcl
terraform apply
```

Leva ~10 min (o CloudFront é o mais demorado). Ao final mostra o `app_url`.

### 4. Ligar o deploy no GitHub

```bash
cd ../../..
infra/scripts/sync-github-vars.sh
```

Copia os outputs para o GitHub Environment `staging` (restrito à branch `dev`) e liga o deploy.

### 5. Primeiro deploy

GitHub → **Actions → Deploy staging → Run workflow** (ou faça qualquer merge na `dev`).

Depois acesse:
- Frontend: `<app_url>`
- API: `<app_url>/api/health` e `<app_url>/api/swagger`

---

## Dia a dia

| Tarefa | Como |
|---|---|
| Deploy | Automático a cada merge na `dev` com CI verde |
| Deploy manual / redeploy | Actions → Deploy staging → Run workflow |
| Terminal na EC2 | Console AWS → EC2 → instância → **Connect → Session Manager** (sem SSH) |
| Logs da API | Na sessão: `cd /opt/fluxus && sudo docker compose logs -f api` |
| Acessar o banco | Na sessão: `sudo docker compose exec postgres psql -U fluxus -d fluxus_manager` |
| Restaurar backup | EC2 → Snapshots → criar volume do snapshot → trocar pelo volume de dados |
| Alterar infra | Edite o Terraform, abra PR (CI valida), depois `terraform apply` em `environments/staging` |
| Desligar tudo | `terraform destroy` em `environments/staging` (o volume de dados tem `prevent_destroy`: remova a trava antes, se for mesmo apagar) |

## Próximos passos

- **Domínio próprio:** certificado ACM em `us-east-1` + `aliases`/`viewer_certificate` no CloudFront.
- **Production:** copiar `environments/staging` para `environments/production`, adicionar o environment
  no bootstrap (`github_environments`) e um workflow com aprovação manual.
