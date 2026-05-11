# Deploying RecipesApp to Azure

## Azure resources

| Resource | Service | SKU |
|---|---|---|
| API | App Service (Linux, .NET 10) | F1 free |
| React frontend | Static Web Apps | Free |
| Angular frontend | Static Web Apps | Free |
| Database | Azure SQL (serverless) | Free tier |
| Secrets | Key Vault | Standard |
| Monitoring | Application Insights | Pay-as-you-go |

Infrastructure is defined as Bicep IaC in `/infra`.

## Prerequisites

- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) installed and authenticated (`az login`)
- [PowerShell 7+](https://learn.microsoft.com/powershell/scripting/install/installing-powershell) (Windows) or `bash` + `jq` + `openssl` (macOS / Linux) — for the bootstrap script
- Azure subscription
- SQL Server admin password (strong, avoid `@` character)
- Anthropic API key (`sk-ant-...`)

## Step 0 — One-time bootstrap

This script creates the resource group, an Azure AD application with GitHub OIDC federated credentials, and a Contributor role assignment scoped to the resource group. Run it once per Azure subscription. It is idempotent — safe to re-run.

After this step, subsequent deployments are driven by GitHub Actions workflows triggered by push to `main`. No more manual `az` commands.

### Windows (PowerShell)

```powershell
pwsh infra/bootstrap.ps1 `
  -ResourceGroup RecipesApp `
  -Location westeurope `
  -Prefix recipes-prod-<unique-suffix> `
  -GitHubRepo your-org/RecipesApp
```

### macOS / Linux (bash)

```bash
infra/bootstrap.sh \
  --resource-group RecipesApp \
  --location westeurope \
  --prefix recipes-prod-<unique-suffix> \
  --github-repo your-org/RecipesApp
```

The script prints the values to set as **GitHub repository variables** (Settings → Secrets and variables → Actions → Variables):

| Variable | Source |
|---|---|
| `AZURE_CLIENT_ID` | bootstrap output |
| `AZURE_TENANT_ID` | bootstrap output |
| `AZURE_SUBSCRIPTION_ID` | bootstrap output |
| `AZURE_RESOURCE_GROUP` | the value you passed as `--resource-group` |
| `AZURE_PREFIX` | the value you passed as `--prefix` |

And as **GitHub repository secrets** (same screen, Secrets tab):

| Secret | Source |
|---|---|
| `SQL_ADMIN_PASSWORD` | choose now |
| `ANTHROPIC_API_KEY` | your Anthropic account |
| `JWT_SIGNING_KEY` | written to `.bootstrap-output.txt` (gitignored) |
| `MCP_SERVER_TOKEN` | written to `.bootstrap-output.txt` (gitignored) |

> The bootstrap script **does not** store any long-lived service principal secret in GitHub. Workflow auth uses GitHub's OIDC token, federated to the AD app the script creates.

### What the bootstrap creates

| Resource | Purpose |
|---|---|
| Resource group | Container for all RecipesApp Azure resources |
| Azure AD app `github-actions-<rg>` | Identity used by GitHub Actions workflows |
| Service principal | Backing identity for role assignments |
| Federated credential `github-main` | Allows OIDC tokens from `refs/heads/main` to authenticate as the SP |
| Federated credential `github-pr` | Allows OIDC tokens from PR runs to authenticate as the SP |
| Role assignment | Contributor on the resource group, scoped to the SP |
| `.bootstrap-output.txt` | Local, gitignored — holds the generated JWT signing key |

### Tearing the bootstrap down

```bash
az ad app delete --id <AZURE_CLIENT_ID>
az group delete --name RecipesApp --yes
```

---

## Step 1 — Deploy infrastructure

Push any change under `infra/**` to `main`. The `infra-deploy` workflow runs `az deployment group create` automatically using OIDC, then writes the deployment outputs (API URL, app URLs, Key Vault name) to the workflow run summary.

```bash
git add infra/
git commit -m "chore(infra): tweak"
git push origin main
```

Watch the run at `https://github.com/<owner>/<repo>/actions/workflows/infra-deploy.yml`. The summary lists every output and tells you which repo variables to populate after the first deploy.

You can also trigger the workflow manually from the Actions tab (`Run workflow` → `infra-deploy`) without changing any code — useful for re-applying after rotating a secret.

After the first run, set these additional repo variables from the summary output:

| Variable | Value |
|---|---|
| `API_HOSTNAME` | `<prefix>-api.azurewebsites.net` |
| `KEY_VAULT_NAME` | `<prefix>-kv` (or truncated to 24 chars) |

<details>
<summary>Manual fallback — if you need to deploy outside GitHub Actions</summary>

```bash
# Resource group (skip if bootstrap was run)
az group create --name RecipesApp --location westeurope

# Deploy
SQL_ADMIN_PASSWORD=<your-password> \
ANTHROPIC_API_KEY=sk-ant-... \
JWT_SIGNING_KEY=<from .bootstrap-output.txt> \
az deployment group create \
  --resource-group RecipesApp \
  --template-file infra/main.bicep \
  --parameters infra/main.bicepparam \
  --parameters prefix=recipes-<unique-suffix>

# Retrieve outputs at any time
az deployment group show \
  --resource-group RecipesApp \
  --name main \
  --query properties.outputs
```

The `prefix` must be globally unique (used in Key Vault and SQL Server names). Add a short random suffix to avoid conflicts, e.g. `recipes-abc123`.

</details>

---

## Step 2 — Deploy app code

Push to `main` under `Backend/**`, `Frontend/**`, or `FrontendAngular/**`. The corresponding CI workflow runs build + tests + deploy in one job graph:

| Path changed | Workflow | What it does |
|---|---|---|
| `Backend/**` | `backend-ci` | Unit + integration tests → `dotnet publish` → `azure/webapps-deploy` → smoke check `/health` |
| `Frontend/**` | `frontend-react-ci` | `npm ci`/`lint`/`build` (with `VITE_API_BASE_URL`) → fetch SWA token from Key Vault → `static-web-apps-deploy` |
| `FrontendAngular/**` | `frontend-angular-ci` | Same as React, plus `sed` to bake API URL into `environment.prod.ts` before `ng build` |

Production deploys run on `push` to `main`. Backend PRs still trigger build + tests but skip the deploy job.

The frontend workflows fetch their SWA deployment tokens from Key Vault at deploy time using the GitHub OIDC trust — no token secrets need to live in GitHub.

### PR previews (frontends only)

Pull requests that touch `Frontend/**` or `FrontendAngular/**` deploy to a per-PR preview environment in Azure Static Web Apps. The deploy action posts a comment on the PR with the preview URL (e.g. `https://<random>-<n>.azurestaticapps.net`). Each push to the PR re-deploys to the same URL.

When the PR is closed (merged or not), the `close-preview` job tears the environment down. The PR preview points at the **production API URL** — useful for visual review, not isolated data.

<details>
<summary>Manual fallback — deploy backend from a developer machine</summary>

```bash
APP_NAME=recipes-<unique-suffix>-api

dotnet publish Backend/src/Recipes.Api \
  --configuration Release \
  --output ./publish

cd publish && zip -r ../api.zip . && cd ..

az webapp deploy \
  --name $APP_NAME \
  --resource-group RecipesApp \
  --src-path api.zip \
  --type zip

curl https://$APP_NAME.azurewebsites.net/health   # should return Healthy
```

</details>

<details>
<summary>Manual fallback — deploy React frontend</summary>

```bash
cd Frontend

# Point at the deployed API
echo "VITE_API_BASE_URL=https://<apiUrl>" > .env.production.local

npm run build

# Fetch deployment token from Key Vault
TOKEN=$(az keyvault secret show \
  --vault-name <KEY_VAULT_NAME> \
  --name React-Deployment-Token \
  --query value -o tsv)

npx @azure/static-web-apps-cli deploy ./dist \
  --deployment-token "$TOKEN" \
  --env production
```

</details>

<details>
<summary>Manual fallback — deploy Angular frontend</summary>

```bash
cd FrontendAngular

sed -i "s|apiBaseUrl: ''|apiBaseUrl: 'https://<apiUrl>'|g" \
  src/environments/environment.prod.ts

npm run build

TOKEN=$(az keyvault secret show \
  --vault-name <KEY_VAULT_NAME> \
  --name Angular-Deployment-Token \
  --query value -o tsv)

npx @azure/static-web-apps-cli deploy \
  ./dist/frontend-angular/browser \
  --deployment-token "$TOKEN" \
  --env production
```

</details>

## Step 3 — Verify

Open the app URLs from the Bicep outputs. Log in with the credentials you registered (demo seeder is disabled in Production mode — register a new account).

Check Application Insights for any startup errors.

## Pull request checks

Beyond build/test/lint, two workflows post PR comments to surface what would change before merge:

| Workflow | Trigger | What it posts |
|---|---|---|
| `infra-validate` | PR touches `infra/**` | Sticky comment with `az deployment group what-if` output — resources that would be created / modified / deleted if this PR were deployed |
| `frontend-react-ci` / `frontend-angular-ci` | PR touches `Frontend/**` or `FrontendAngular/**` | Comment from the SWA action with a preview URL — torn down on PR close |

Both are sticky: subsequent pushes update the existing comment in place rather than spamming the thread.

## Redeploying after changes

Every redeploy is a `git push` — the workflows handle the rest.

| Changed | Workflow triggered |
|---|---|
| `infra/**` | `infra-deploy` |
| `infra/modules/app-service.bicep` (settings) | `infra-deploy` (idempotent — only changed settings update) |
| `Backend/**` | `backend-ci` (test + deploy) |
| `Frontend/**` | `frontend-react-ci` (build + deploy) |
| `FrontendAngular/**` | `frontend-angular-ci` (build + deploy) |

## Tearing down

```bash
az group delete --name RecipesApp --yes --no-wait
```

This deletes all resources in the group.
