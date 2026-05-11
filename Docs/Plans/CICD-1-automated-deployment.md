# CICD-1 — Automated Deployment

## Context

Today the deploy guide has 7 sequential manual `az` commands and stores a long-lived
`AZURE_CREDENTIALS` service principal in GitHub secrets. This plan turns that into
push-to-deploy: a single bootstrap script runs once, after which every push to `main`
deploys the right thing, and PRs get preview URLs and infra diffs as comments.

See `Docs/specs/CICD-1-automated-deployment.md` for the full spec — problem, design, and
acceptance criteria.

**Six bundles, in dependency order:**

```
1 → 4 → 2 → 3 → 5 → 10
=
CICD-1-1 → CICD-1-2 → CICD-1-3 → CICD-1-4 → CICD-1-5 → CICD-1-6
```

The first two bundles are mostly local file work (script + Bicep). The deploy workflows
(CICD-1-3, CICD-1-4) can only be exercised end-to-end on a real Azure subscription, so
they include a "verification" path that runs the bootstrap once on a throwaway RG.

---

## Bundle CICD-1-1 — OIDC bootstrap script (≈ 60 minutes)

**Goal:** A single script creates the resource group, an Azure AD app with GitHub OIDC
federation, and prints the values to set as GitHub repo variables. No long-lived
secrets. Idempotent — safe to re-run.

Steps:

1. Create `infra/bootstrap.ps1`:
   - Args: `-ResourceGroup`, `-Location`, `-Prefix`, `-GitHubRepo` (e.g. `owner/repo`)
   - Generates a 32-byte base64 JWT signing key
   - `az group create` (idempotent)
   - `az ad app create` if app doesn't exist; capture appId / objectId
   - `az ad sp create` for the app if missing
   - `az role assignment create --role Contributor --scope <rg-id>` for the SP
   - `az ad app federated-credential create` for two subjects:
     - `repo:<owner>/<repo>:ref:refs/heads/main`
     - `repo:<owner>/<repo>:pull_request`
   - Writes the JWT signing key to `./.bootstrap-output.txt` (gitignored) so the
     operator can copy it into the `JWT_SIGNING_KEY` GitHub secret
   - Prints a "Next steps" block listing exactly which GitHub variables and secrets to
     set, with the values inline
2. Add `infra/bootstrap.sh` — a thin POSIX equivalent for macOS / Linux operators. Same
   inputs and outputs.
3. Add `.bootstrap-output.txt` to `.gitignore`.
4. Update `Docs/deployment/azure.md`:
   - Replace the existing 7-step manual flow with a much shorter one:
     1. Run `pwsh infra/bootstrap.ps1 -ResourceGroup ... -Prefix ... -GitHubRepo ...`
     2. Paste the printed values into GitHub repo variables and secrets
     3. Push a commit to `main`
   - Keep an "appendix: what bootstrap does under the hood" section for transparency.

**Verification:** Run the script on a real Azure subscription against a throwaway repo
(or a test fork). Re-run it — the second invocation should be a no-op (no errors). Check
in the Azure portal that the AD app exists, has two federated credentials, and has
`Contributor` on the RG.

---

## Bundle CICD-1-2 — App settings into Bicep (≈ 45 minutes)

**Goal:** every value currently set with `az webapp config appsettings set` lives in
Bicep. JWT signing key stored as a Key Vault secret, referenced from App Service.

Steps:

1. Edit `infra/main.bicep`:
   - Add `@secure() param jwtSigningKey string` near `anthropicApiKey`
   - Pass `jwtSigningKey` into the `keyVault` module call
2. Edit `infra/modules/key-vault.bicep`:
   - Add a `jwtSigningKey` `@secure()` param
   - Add a third `Microsoft.KeyVault/vaults/secrets` resource named `Jwt--SigningKey`
   - Output `jwtSigningKeySecretUri` for symmetry
3. Edit `infra/modules/app-service.bicep`:
   - Expand `appSettings:` from 4 entries to ~15 — see the spec's "App Service module"
     section for the exact list. Includes `Database__Provider`, `Seed__Enabled=false`,
     all 8 `*:Provider=Claude` switches, all 4 `Jwt__*` settings.
4. Edit `infra/main.bicepparam`:
   - Add `param jwtSigningKey = readEnvironmentVariable('JWT_SIGNING_KEY')`
5. Update `Docs/deployment/azure.md`:
   - Delete the entire "Step 3 — Configure the App Service" section
   - The bootstrap output now mentions `JWT_SIGNING_KEY` as a required env var for the
     `az deployment group create` call

**Verification:** Run `az bicep build infra/main.bicep` — no errors. If a real
deployment was previously run, redeploy: `az deployment group create ...`. Confirm in
the Azure portal that the App Service has all 15 settings, that
`Jwt__SigningKey`, `Claude__ApiKey`, and `ConnectionStrings__RecipesDb` show as
"Key Vault Reference" with green checkmarks.

---

## Bundle CICD-1-3 — Infra-deploy workflow (≈ 30 minutes)

**Goal:** push to `main` touching `infra/**` runs `az deployment group create`
automatically. No manual `az` ever again for infra changes.

Steps:

1. Create `.github/workflows/infra-deploy.yml`:
   - Triggers: `push` to `main` with `paths: ['infra/**', '.github/workflows/infra-deploy.yml']`; also `workflow_dispatch`
   - `permissions: { id-token: write, contents: read }`
   - Single job:
     - `actions/checkout@v4`
     - `azure/login@v2` with `client-id`, `tenant-id`, `subscription-id` from `vars`
     - `az deployment group create` with template + bicepparam, passing
       `sqlAdminPassword`, `anthropicApiKey`, `jwtSigningKey` from `secrets`, and
       `prefix` from `vars.AZURE_PREFIX`
     - Capture deployment outputs to `$GITHUB_STEP_SUMMARY` so the operator can see
       `apiUrl`, deployment tokens, etc. in the workflow run page
2. Update `Docs/deployment/azure.md`:
   - "Step 2 — Deploy infrastructure" now says "push your `infra/**` change to `main`,
     watch the workflow run, copy the outputs from the run summary".

**Verification:** Push a trivial whitespace change to `infra/main.bicep` to `main`. The
workflow runs, completes green, and the run summary shows the deployment outputs.

---

## Bundle CICD-1-4 — Backend + frontend deploy jobs (≈ 90 minutes)

**Goal:** push to `main` touching `Backend/**`, `Frontend/**`, or `FrontendAngular/**`
deploys the corresponding artefact to Azure. PRs still build but don't deploy
(production deploys only on `main`).

Steps:

1. Edit `.github/workflows/backend-ci.yml`:
   - Add a `deploy` job:
     - `if: github.ref == 'refs/heads/main' && github.event_name == 'push'`
     - `needs: [unit, integration]`
     - `permissions: { id-token: write, contents: read }`
     - Steps:
       1. `actions/checkout@v4`
       2. `actions/setup-dotnet@v4` (`dotnet-version: 10.0.x`)
       3. `dotnet publish Backend/src/Recipes.Api -c Release -o ./publish`
       4. `azure/login@v2` (OIDC)
       5. `azure/webapps-deploy@v3` with `app-name: ${{ vars.AZURE_PREFIX }}-api` and
          `package: ./publish`
       6. Smoke check: `curl --fail https://${{ vars.API_HOSTNAME }}/health`
2. Edit `.github/workflows/frontend-react-ci.yml`:
   - Add a `deploy` job, `if: github.ref == 'refs/heads/main' && github.event_name == 'push'`
   - Steps:
     1. Checkout, setup-node, `npm ci`
     2. `npm run build` with `VITE_API_BASE_URL=https://${{ vars.API_HOSTNAME }}` set as env
     3. `azure/login@v2`
     4. Pull SWA deployment token from Key Vault:
        `az keyvault secret show --vault-name ${{ vars.KEY_VAULT_NAME }} --name React-Deployment-Token --query value -o tsv`
        (The token is stashed there by the bootstrap or first infra deploy.)
     5. `Azure/static-web-apps-deploy@v1` with the token, `app_location: Frontend/dist`,
        `skip_app_build: true`, `production_branch: main`
3. Edit `.github/workflows/frontend-angular-ci.yml`:
   - Same pattern as React, but the API URL has to be baked into
     `src/environments/environment.prod.ts` before `npm run build` (Angular's
     environment files are static at build time):
     ```bash
     sed -i "s|apiBaseUrl: ''|apiBaseUrl: 'https://${{ vars.API_HOSTNAME }}'|g" \
       src/environments/environment.prod.ts
     ```
   - Or — preferred — extend `environment.prod.ts` to read from `process.env` via a
     build-time replacement. For this bundle, sed is fine.
4. Update Bicep — push the SWA deployment tokens into Key Vault automatically. In
   `infra/main.bicep`, after the SWA modules deploy, add two more secrets to the Key
   Vault: `React-Deployment-Token` and `Angular-Deployment-Token`. This eliminates a
   manual GitHub-secret step.
5. Update `Docs/deployment/azure.md`:
   - "Steps 4–6" (backend and frontend deploys) collapse into "push to `main`".

**Verification:** Push three independent commits — one each to `Backend/`, `Frontend/`,
`FrontendAngular/`. Each triggers its own workflow only. Each deploys, smoke check
passes, hosted URLs return live content.

---

## Bundle CICD-1-5 — SWA preview environments for PRs (≈ 30 minutes)

**Goal:** opening a PR that touches a frontend produces a preview URL automatically,
posted as a PR comment. Closing the PR tears it down.

Steps:

1. Edit `.github/workflows/frontend-react-ci.yml`:
   - Adjust the `deploy` job's `if:` to also run on `pull_request` events
   - Add a separate `close` job that runs on `pull_request` `closed` event with
     `Azure/static-web-apps-deploy@v1` action set to `action: close`
   - The action's PR comment behaviour is automatic — it picks up the PR number from the
     event context and posts a preview URL comment
2. Edit `.github/workflows/frontend-angular-ci.yml` — same pattern as React.
3. Permissions: both workflows need `pull-requests: write` so the action can comment.
4. PR previews use the production-ish API URL (`vars.API_HOSTNAME`) — they're
   read-only-ish during the PR. Acceptable for now; isolating PR data is out of scope.

**Verification:** Open a draft PR that changes a single visible string in
`Frontend/src/locales/en.json`. Within ~3 minutes, the SWA action posts a comment with a
`https://<random>-<n>.azurestaticapps.net` URL. Click it; the change is visible. Close
the PR; the preview URL stops resolving.

---

## Bundle CICD-1-6 — `what-if` PR check on infra changes (≈ 30 minutes)

**Goal:** PRs that touch `infra/**` get a comment showing the resource diff, the same
way `claude-review` posts review comments. Catches Bicep mistakes before merge.

Steps:

1. Create `.github/workflows/infra-validate.yml`:
   - Trigger: `pull_request` with `paths: ['infra/**']`
   - `permissions: { id-token: write, contents: read, pull-requests: write }`
   - Steps:
     1. Checkout
     2. `azure/login@v2` (OIDC)
     3. `az deployment group what-if --no-pretty-print -o json` against the same
        template + parameters as the production deploy. Capture stdout to a file.
     4. Format the JSON output as a Markdown comment (a small `node` or `pwsh`
        one-liner; or use a marketplace action like
        `Azure/cli@v2` + `peter-evans/create-or-update-comment@v4`)
     5. Post or update a sticky comment on the PR using
        `peter-evans/create-or-update-comment@v4` with `comment-id` keyed by a fixed
        marker so re-runs replace the previous comment
2. Add a separate federated credential subject for `pull_request` if not already set up
   in CICD-1-1. (CICD-1-1's bootstrap script already includes this, so no change needed
   here unless the operator skipped it.)
3. Update `Docs/deployment/azure.md` — mention the PR comment behaviour.

**Verification:** Open a PR that adds a tag to `infra/main.bicep`. The
`infra-validate.yml` workflow runs, completes green, posts a comment showing the
single-resource diff (the tag change). Push another commit; the comment updates rather
than duplicating.

---

## Files to modify (cross-bundle index)

| Path | Bundle |
|---|---|
| `infra/bootstrap.ps1` (new) | CICD-1-1 |
| `infra/bootstrap.sh` (new) | CICD-1-1 |
| `.gitignore` | CICD-1-1 |
| `Docs/deployment/azure.md` | CICD-1-1 → CICD-1-6 |
| `infra/main.bicep` | CICD-1-2, CICD-1-4 |
| `infra/main.bicepparam` | CICD-1-2 |
| `infra/modules/key-vault.bicep` | CICD-1-2, CICD-1-4 |
| `infra/modules/app-service.bicep` | CICD-1-2 |
| `.github/workflows/infra-deploy.yml` (new) | CICD-1-3 |
| `.github/workflows/backend-ci.yml` | CICD-1-4 |
| `.github/workflows/frontend-react-ci.yml` | CICD-1-4, CICD-1-5 |
| `.github/workflows/frontend-angular-ci.yml` | CICD-1-4, CICD-1-5 |
| `.github/workflows/infra-validate.yml` (new) | CICD-1-6 |

---

## Recommended execution order

> **CICD-1-1 → CICD-1-2 → CICD-1-3 → CICD-1-4 → CICD-1-5 → CICD-1-6**

Rationale:
- Bootstrap (CICD-1-1) has to come first — every later bundle assumes OIDC trust exists.
- App settings into Bicep (CICD-1-2) before infra-deploy (CICD-1-3): once the workflow
  runs, you want all settings to deploy in one shot, otherwise you'll reintroduce manual
  steps.
- Infra-deploy (CICD-1-3) before app deploys (CICD-1-4): the app workflows depend on the
  resources existing — App Service name, Key Vault name, SWA deployment tokens.
- Previews (CICD-1-5) require CICD-1-4 to be working first.
- `what-if` (CICD-1-6) is fully independent of CICD-1-4/5 but fits naturally last.

Each bundle is a single commit. One PR for the whole feature, or one PR per bundle —
operator choice. Lean toward one PR per bundle so reviewers can sanity-check each
incremental claim against a green workflow run.

---

## End-to-end verification

After all six bundles ship:

1. **Fresh setup test** (run on a throwaway RG, ideally a separate Azure subscription):
   ```bash
   pwsh infra/bootstrap.ps1 \
     -ResourceGroup RecipesAppTest \
     -Location westeurope \
     -Prefix recipes-test-xyz \
     -GitHubRepo your-fork/RecipesApp
   ```
   - Paste the printed values into GitHub repo variables and secrets.
   - Push a commit to `main`. Watch four workflows run green: `infra-deploy`,
     `backend-ci`, `frontend-react-ci`, `frontend-angular-ci`.
   - Visit the URLs from the `infra-deploy` run summary. Backend `/health` returns
     `Healthy`. Both frontends load.

2. **PR preview test:**
   - Open a PR changing one string in `Frontend/src/locales/en.json`. Within 3 minutes,
     the SWA action comments a preview URL. Click — change visible.
   - Close the PR. Preview URL stops resolving.

3. **Infra what-if test:**
   - Open a PR adding `tags: { extra: 'test' }` to a resource in
     `infra/modules/app-service.bicep`. The `infra-validate` workflow comments a
     resource diff showing only the tag change. Force-push another commit; the comment
     updates in place.

4. **Idempotency test:**
   - Re-run the bootstrap script. No errors, no duplicate AD apps, no duplicate role
     assignments.
   - Re-run `infra-deploy.yml` via `workflow_dispatch`. Bicep reports "no changes" for
     resources, secrets stay unchanged.

5. **No long-lived secrets test:**
   - GitHub repo settings → Secrets → confirm no `AZURE_CREDENTIALS`. Only
     `SQL_ADMIN_PASSWORD`, `ANTHROPIC_API_KEY`, `JWT_SIGNING_KEY` remain.

6. **Tear-down:**
   - `az group delete --name RecipesAppTest --yes`
   - In Azure AD, delete the test app registration (the bootstrap script left a comment
     in its output explaining how to do this).

Acceptance: all 9 acceptance criteria from `Docs/specs/CICD-1-automated-deployment.md`
pass.

---

## Review pass (after implementation, before merge)

Manual checklist:

- [ ] `infra/bootstrap.ps1` is idempotent — second run produces no warnings or errors,
      and creates no duplicates.
- [ ] No `AZURE_CREDENTIALS` mentioned anywhere in `.github/workflows/**`.
- [ ] All `azure/login@v2` calls use OIDC (`client-id` / `tenant-id` /
      `subscription-id`), not `creds:`.
- [ ] Every workflow that touches Azure has `permissions: { id-token: write }`.
- [ ] `Docs/deployment/azure.md` has no manual `az webapp config appsettings set` calls.
- [ ] Bicep files build cleanly: `az bicep build infra/main.bicep` produces no warnings.
- [ ] Frontend deploy jobs run only on push to `main`, not on every PR (otherwise PRs
      race with production).
- [ ] PR preview deploys for frontends use the SWA action's built-in preview behaviour,
      not a copy of the production-deploy job with a different `production_branch`.
- [ ] `what-if` comment is sticky (one comment per PR, updated in place).
- [ ] `.bootstrap-output.txt` is in `.gitignore`.

If review surfaces issues, fix in-place and re-verify against the end-to-end checklist
above.
