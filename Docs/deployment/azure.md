# Deploying RecipesApp to Azure

## Azure resources

| Resource | Service | SKU |
|---|---|---|
| API | App Service (Linux, .NET 10) | F1 free |
| MCP server | App Service (Linux, .NET 10) | F1 free (shared plan with API) |
| React frontend | Static Web Apps | Free |
| Angular frontend | Static Web Apps | Free |
| Database | Azure SQL (serverless) | Free tier |
| Secrets | Key Vault | Standard |
| Monitoring | Application Insights | Pay-as-you-go |

Infrastructure is defined as Bicep IaC in `/infra`.

> **Free tier limits to watch:**
> - SQL pauses after 60 s of inactivity (auto-resumes on first query, adds ~5 s latency)
> - App Service F1 has 60 CPU minutes/day — fine for testing, but the app sleeps after 20 min of inactivity
> - Key Vault allows 2 000 secret operations / 10 min on the free tier, well within normal usage

---

## Prerequisites

- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) installed  
- [PowerShell 7+](https://learn.microsoft.com/powershell/scripting/install/installing-powershell) (Windows) or `bash` + `jq` + `openssl` (macOS / Linux)  
- An Azure subscription (free trial works)  
- An Anthropic API key (`sk-ant-...`)  
- Your GitHub repo accessible (public, or Actions enabled)

Log in to Azure before running any steps:

```powershell
az login
az account show   # confirm it shows the correct subscription
```

---

## Step 1 — Choose a unique prefix

The prefix becomes part of all resource names (Key Vault, SQL Server, App Service hostname). Requirements:

- Globally unique across Azure (SQL Server and Key Vault names are public DNS)
- Lowercase letters, numbers, and hyphens only
- 20 characters or fewer

Pick something like `recipes-<initials>-<4-digit-random>`, e.g. `recipes-ss-7421`.

---

## Step 2 — Run the bootstrap script

From the repo root, run the bootstrap script once. It is idempotent — safe to re-run.

### Windows (PowerShell)

```powershell
pwsh infra/bootstrap.ps1 `
  -ResourceGroup RecipesApp `
  -Location westeurope `
  -Prefix recipes-ss-7421 `
  -GitHubRepo YourGitHubUsername/RecipesApp
```

### macOS / Linux (bash)

```bash
infra/bootstrap.sh \
  --resource-group RecipesApp \
  --location westeurope \
  --prefix recipes-ss-7421 \
  --github-repo YourGitHubUsername/RecipesApp
```

Replace `recipes-ss-7421` with your chosen prefix and `YourGitHubUsername/RecipesApp` with your actual GitHub repo path.

**What the script does:**

| Resource | Purpose |
|---|---|
| Resource group `RecipesApp` | Container for all Azure resources |
| Azure AD app `github-actions-RecipesApp` | Identity used by GitHub Actions workflows |
| Service principal | Backing identity for role assignments |
| Federated credential `github-main` | Allows OIDC tokens from `refs/heads/main` to authenticate as the SP |
| Federated credential `github-pr` | Allows OIDC tokens from PR runs to authenticate as the SP |
| Role assignment: Contributor | Scoped to the resource group |
| Role assignment: Key Vault Secrets User | Scoped to the resource group |
| Role assignment: User Access Administrator | Scoped to the resource group — required so the SP can create the role-assignment resources in `main.bicep` |
| `.bootstrap-output.txt` | Local, gitignored — holds generated JWT signing key and MCP server token |

> The script does **not** store any long-lived service principal secret in GitHub. Workflow auth uses GitHub's OIDC token federated to the AD app.

**Keep the terminal open** — you will copy values from it in the next step.

---

## Step 3 — Configure GitHub repo secrets and variables

Go to your GitHub repo → **Settings → Secrets and variables → Actions**.

### Variables tab — add 5 variables

| Name | Value |
|---|---|
| `AZURE_CLIENT_ID` | printed by bootstrap as `AZURE_CLIENT_ID` |
| `AZURE_TENANT_ID` | printed by bootstrap as `AZURE_TENANT_ID` |
| `AZURE_SUBSCRIPTION_ID` | printed by bootstrap as `AZURE_SUBSCRIPTION_ID` |
| `AZURE_RESOURCE_GROUP` | `RecipesApp` |
| `AZURE_PREFIX` | your prefix, e.g. `recipes-ss-7421` |

### Secrets tab — add 4 secrets

| Name | Value |
|---|---|
| `SQL_ADMIN_PASSWORD` | A strong password — **no `@` character** (breaks connection strings). Example: `RecipesApp!2024xQ9` |
| `ANTHROPIC_API_KEY` | Your `sk-ant-...` key |
| `JWT_SIGNING_KEY` | From `.bootstrap-output.txt` in repo root |
| `MCP_SERVER_TOKEN` | From `.bootstrap-output.txt` in repo root |

To read `.bootstrap-output.txt`:

```powershell
Get-Content .bootstrap-output.txt
```

---

## Step 4 — Deploy infrastructure

Trigger the `infra-deploy` workflow. Two options:

**Option A — manual trigger (recommended for first deploy):**  
GitHub → Actions → `infra-deploy` → **Run workflow** → Run.

**Option B — push an empty commit:**

```powershell
git commit --allow-empty -m "chore: trigger infra deploy"
git push origin main
```

The workflow runs `az deployment group create` via OIDC and deploys all Azure resources:

- SQL Server + database (free tier, auto-pause on idle)
- Key Vault with all secrets pre-populated
- App Service plan (F1) + API app + MCP server app (sharing the same plan)
- Static Web Apps for React and Angular
- Application Insights
- RBAC role assignments for managed identities

**Expected duration: 15–20 minutes.**

When it finishes, the workflow run **Summary** tab shows:

```
| API URL         | https://recipes-ss-7421-api.azurewebsites.net       |
| MCP server URL  | https://recipes-ss-7421-mcp.azurewebsites.net       |
| React app URL   | https://<random>.azurestaticapps.net                |
| Angular app URL | https://<random>.azurestaticapps.net                |
| Key Vault name  | recipes-ss-7421-kv                                  |

Set these as GitHub repo variables (first deploy only):
  API_HOSTNAME   = recipes-ss-7421-api.azurewebsites.net
  KEY_VAULT_NAME = recipes-ss-7421-kv
```

<details>
<summary>Manual fallback — deploy infrastructure outside GitHub Actions</summary>

```powershell
$env:SQL_ADMIN_PASSWORD  = "<your-password>"
$env:ANTHROPIC_API_KEY   = "sk-ant-..."
$env:JWT_SIGNING_KEY      = "<from .bootstrap-output.txt>"
$env:MCP_SERVER_TOKEN     = "<from .bootstrap-output.txt>"

az deployment group create `
  --resource-group RecipesApp `
  --template-file infra/main.bicep `
  --parameters infra/main.bicepparam `
  --parameters prefix=recipes-ss-7421 `
  --query properties.outputs `
  --output json
```

</details>

---

## Step 5 — Set the two post-deploy variables

Back in GitHub → **Settings → Secrets and variables → Actions → Variables**, add:

| Name | Value (from run summary) |
|---|---|
| `API_HOSTNAME` | `recipes-ss-7421-api.azurewebsites.net` |
| `KEY_VAULT_NAME` | `recipes-ss-7421-kv` |

These are needed by the frontend build (to bake in the API URL) and by all deploy workflows to fetch SWA tokens from Key Vault.

---

## Step 6 — Deploy app code

Trigger all three app CI workflows with a push to `main`, or run them manually from the Actions tab:

```powershell
git commit --allow-empty -m "chore: trigger app deploys"
git push origin main
```

This kicks off in parallel:

| Workflow | What it does |
|---|---|
| `backend-ci` | Build → unit + integration tests → `dotnet publish` → deploy API + MCP server → smoke-check `/health` |
| `frontend-react-ci` | `npm ci` → lint → build (with API URL baked in) → fetch SWA token from Key Vault → deploy |
| `frontend-angular-ci` | `npm ci` → lint → build → Playwright E2E tests → fetch SWA token → deploy |

**Expected duration: 10–15 minutes.**

> **Key Vault RBAC propagation:** If the backend deploy fails with a Key Vault 403 on the first deploy, wait 5 minutes and re-run the workflow. RBAC role assignments take a few minutes to propagate after a fresh infra deploy.

<details>
<summary>Manual fallback — deploy backend from a developer machine</summary>

```bash
APP_NAME=recipes-ss-7421-api

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

echo "VITE_API_BASE_URL=https://recipes-ss-7421-api.azurewebsites.net" > .env.production.local

npm run build

TOKEN=$(az keyvault secret show \
  --vault-name recipes-ss-7421-kv \
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

sed -i "s|apiBaseUrl: ''|apiBaseUrl: 'https://recipes-ss-7421-api.azurewebsites.net'|g" \
  src/environments/environment.prod.ts

npm run build

TOKEN=$(az keyvault secret show \
  --vault-name recipes-ss-7421-kv \
  --name Angular-Deployment-Token \
  --query value -o tsv)

npx @azure/static-web-apps-cli deploy \
  ./dist/frontend-angular/browser \
  --deployment-token "$TOKEN" \
  --env production
```

</details>

---

## Step 7 — Verify

Open the React or Angular app URL from the infra run summary.

1. **Register a new account** — the production seeder is disabled; use the Register page
2. **Browse recipes** — the list should load from the live SQL database
3. **AI features** — try ingredient substitution or meal plan generation (these call your Anthropic key)
4. **Check Application Insights** — in the Azure Portal, open the Application Insights resource (`<prefix>-insights`) and look at the Live Metrics or Failures blade for any startup errors

---

## Total time: ~45–50 minutes end to end

| Step | Time |
|---|---|
| Prerequisites + az login | 5 min |
| Choose prefix | 2 min |
| Bootstrap script | 10 min |
| GitHub secrets + variables | 10 min |
| Infra deploy | 15–20 min |
| Post-deploy variables | 2 min |
| App code deploy | 10–15 min |
| Verify | 5 min |

The only manual steps are running bootstrap once and entering 11 values into GitHub. Everything after that is automated by `git push`.

---

## Pull request checks

Beyond build/test/lint, two workflows post PR comments to surface what would change before merge:

| Workflow | Trigger | What it posts |
|---|---|---|
| `infra-validate` | PR touches `infra/**` | Sticky comment with `az deployment group what-if` output — resources that would be created / modified / deleted |
| `frontend-react-ci` / `frontend-angular-ci` | PR touches `Frontend/**` or `FrontendAngular/**` | Comment from the SWA action with a preview URL — torn down on PR close |

Both are sticky: subsequent pushes update the existing comment in place rather than spamming the thread.

---

## Redeploying after changes

Every redeploy is a `git push` — the workflows handle the rest.

| Changed | Workflow triggered |
|---|---|
| `infra/**` | `infra-deploy` |
| `Backend/**` | `backend-ci` (test + deploy API + MCP server) |
| `Frontend/**` | `frontend-react-ci` (build + deploy) |
| `FrontendAngular/**` | `frontend-angular-ci` (build + E2E + deploy) |

To re-apply infra after rotating a secret (e.g. new `SQL_ADMIN_PASSWORD`), trigger `infra-deploy` manually from the Actions tab — no code change needed.

---

## Tearing down

```bash
# Delete all Azure resources
az group delete --name RecipesApp --yes --no-wait

# Delete the OIDC trust (AD app)
az ad app delete --id <AZURE_CLIENT_ID>
```

This deletes all resources in the group including the SQL database, Key Vault, App Service, and Static Web Apps.
