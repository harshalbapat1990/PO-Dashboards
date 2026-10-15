# Product Health Dashboard

The Product Health Dashboard is being delivered in stages. The target client is
the existing Vue 3 app in `web/`; the Azure Functions .NET isolated worker in
`api/DesignAutomation.AppCatalog/` is the application API; PostgreSQL is the
data store. Existing MongoDB records are intentionally abandoned. MongoDB,
Docker, and old-data migration are not part of the development setup.

## Local prerequisites

- .NET SDK **10.0.400** (pinned in `global.json`)
- Node.js **24.13.0** or later and npm
- PostgreSQL running locally; the current development database is
  `dashboard_dev` at `localhost:5433`
- Azurite for the local Azure Functions host storage emulator

Local API configuration must use loopback PostgreSQL and Azurite. Local mode
fails closed if those settings are not present. It does not instantiate the
GitHub client, call OAuth/identity endpoints, use MongoDB, or emit telemetry.
GitHub/Azure DevOps references entered in setup are stored as configuration;
no provider sync runs in Stage 0.

## Configure PostgreSQL and API

1. Create `api/DesignAutomation.AppCatalog/local.settings.json` from
   `api/DesignAutomation.AppCatalog/local.settings.template.json`.
2. Set `PostgreSqlConnectionString` to your local account and
   `dashboard_dev` database. Never commit this file or put database credentials
   in Vue configuration.
3. Generate a base64 `TokenKey` from 32 random bytes and set a private
   `LocalAdminPassword` of at least 20 characters. Set `LocalAdminEmail` to the
   local development identity. These values are used only by the local API;
   the local token endpoint is disabled outside `Environment=Local`.
   In PowerShell, this creates values locally (copy them directly into the
   ignored settings file; do not send them in chat or commit them):

   ```powershell
   $keyBytes = New-Object byte[] 32
   $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
   $rng.GetBytes($keyBytes)
   [Convert]::ToBase64String($keyBytes)
   $rng.Dispose()
   ```

   Generate a separate local password with the same command and a separate
   random byte array, or use a password manager to create a unique value.
4. Ensure `AzureWebJobsStorage` is `UseDevelopmentStorage=true` and start
   Azurite.
5. Apply SQL migrations in filename order using DBeaver or `psql`:
   `001_stage0_foundation.sql` through `005_github_record_relationships.sql`.
   Each script is safe to reapply and records its version in `schema_migrations`.
   Migration 002 drops the unused legacy `apps` table and renames the minimal
   identity table for Product Health. Back up first; old App Catalog data is
   intentionally abandoned.
6. Start the API in a PowerShell window:

   ```powershell
   $env:FUNCTIONS_CORE_TOOLS_TELEMETRY_OPTOUT = "1"
   Set-Location api/DesignAutomation.AppCatalog
   dotnet run
   ```

The local host listens at `http://localhost:7071`. The Product Health API
includes local sign-in, foundation/product and access management, per-product
dashboard configuration, and GitHub snapshot read/import routes. Product
endpoints require a signed JWT with the relevant product or portfolio grant.
The local token endpoint is enabled only with `Environment=Local`; it validates
the configured email/password and creates or updates the local admin identity
in PostgreSQL. Retired App Catalog and external OAuth endpoints have been
removed.

## GitHub data in local development

The `/github-data` Vue view reads repository, branch, commit, pull request,
release, Actions, and security records stored in PostgreSQL. The local API does
not contact GitHub and has no GitHub token setting. To load a source snapshot,
an Admin may POST a normalized snapshot to:

`POST /api/v1/foundation/products/{productId}/github/import`

The JSON body contains `repositoryUrl` and a `records` array. Each record has
`entityType`, `externalId`, optional `parentExternalId`, and a `data` object with
the normalized fields defined in
`PostgresGitHubSourceDataRepository.cs`. Supported types are `repository`,
`branch`, `commit`, `pull_request`, `release`, `workflow`, `workflow_run`, and
`security_alert`. Imports are transactional and repeatable; records must belong
to a repository already linked to the product. A normalized record import is
available only in Local mode and requires an Admin JWT. Read requests are
product-authorized and paginated. `POST /api/v1/foundation/products/{productId}/github/sync`
is available outside Local mode to Admins and product/portfolio managers. It
fetches only a repository already linked to that product, stores the normalized
snapshot, and accepts no token in the request. Configure `GitHubAccessToken`
only in the deployment's secret environment (for example, a Key Vault-backed
app setting); the API rejects that setting in Local mode. Use a fine-grained
read-only token with repository Metadata/Contents, Pull requests, Actions,
Dependabot alerts, Code scanning alerts, and Secret scanning alerts read
permissions, restricted to the repositories the dashboard needs. See GitHub's
official endpoint documentation for [branches and contents](https://docs.github.com/en/rest/branches/branches),
[pull requests](https://docs.github.com/en/rest/pulls/pulls),
[Actions](https://docs.github.com/en/rest/actions/workflows),
[Dependabot](https://docs.github.com/en/rest/dependabot/alerts),
[code scanning](https://docs.github.com/en/rest/code-scanning/code-scanning), and
[secret scanning](https://docs.github.com/en/rest/secret-scanning/secret-scanning).
The connector follows GitHub's [pagination links](https://docs.github.com/en/rest/using-the-rest-api/using-pagination-in-the-rest-api)
and rate-limit retry guidance (including `Retry-After`); it never persists the
secret value returned by secret scanning.

## Configure and run Vue

1. In `web/`, create ignored `.env.local` for non-secret client settings only
   if needed. During `npm run dev`, API base URLs are forced to the Vite
   same-origin `/api` proxy targeting `localhost:7071`; remote API URLs and
   client-side Function keys are ignored in development.
2. Start Vue in another PowerShell window:

   ```powershell
   Set-Location web
   npm install
   npm run dev
   ```

3. Open the local Vite URL printed by npm and navigate to `/foundation`.
   Sign in using `LocalAdminEmail` and `LocalAdminPassword` from the ignored API
   settings file. Create an organization, portfolio, product, teams, and
   optional provider references. No GitHub or Azure DevOps connection is made.

## Database backup, recovery, and reset

Before schema changes or sample-data reset, create a PostgreSQL custom-format
backup from DBeaver: right-click `dashboard_dev` → **Tools** → **Backup**, use
custom format, and save the file outside the repository. Restore through
DBeaver's **Tools → Restore** into a separate empty database first, then verify
`schema_migrations` and row counts before using it. Do not overwrite the only
backup.

Migrations are forward-only, idempotent scripts. If an apply fails, retain the
database, capture the error, restore the backup into a scratch database if
needed, and correct/prepare a new migration; do not mark the version manually.
To reset a disposable development database, create a backup first, stop the
API, and use DBeaver to drop and recreate the `public` schema, then rerun all
SQL migrations in order. This permanently deletes that schema's data and must
only be used for the local development database.

There is no MongoDB rollback path because the old data is intentionally not
retained or migrated. Recovery means restoring a PostgreSQL backup or
reinitializing the disposable local database.

## Stage tracking

See `Product_Health_Dashboard_Progress_Plan.txt` for task status, evidence,
open acceptance checks, and the stage delivery sequence. A successful build or
host startup alone does not verify API/database workflows, authentication,
network isolation, or UI behavior.
