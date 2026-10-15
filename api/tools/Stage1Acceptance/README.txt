Foundation, dashboard configuration, and GitHub snapshot acceptance runner
==========================================================================

Purpose
-------
Exercises the Stage 1 hierarchy, product validation, and access rules, plus
Stage 2 dashboard configuration and Stage 3 local GitHub snapshot persistence,
field mappings, parent relationships, pagination contract, input validation,
and authorization against the local Functions API and PostgreSQL database.
It applies the idempotent Stage 3 migrations 004 and 005, logs into the local
Admin account, creates uniquely named test records, and deletes the temporary
product hierarchy and users in a finally block.
It prints test names and HTTP statuses only; it never prints local settings
values or tokens. It does not call GitHub, Azure DevOps, or other services.

Run
---
1. Start the API using the current source and local.settings.json:
   Set-Location api/DesignAutomation.AppCatalog
   dotnet run

2. From a second PowerShell window at the repository root, run:
   dotnet run --project api/tools/Stage1Acceptance

By default the runner targets http://localhost:7071/api/v1/. To target another
local Functions port, set STAGE1_API_ROOT, for example:
   $env:STAGE1_API_ROOT = 'http://localhost:7072/api/v1/'

The database user in local.settings.json needs permission to create the Stage 3
tables/indexes and insert/update/delete temporary acceptance records. The
runner never calls GitHub and verifies that the live sync route is disabled in
Local mode. It should end with “Foundation, dashboard configuration, and local
GitHub snapshot acceptance scenarios all passed.” and “Temporary acceptance
records cleaned up.”
