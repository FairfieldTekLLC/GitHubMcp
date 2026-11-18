# HQ.GitHubMCP

Production-oriented ASP.NET Core 10 MVC starter solution for GitHub Enterprise Cloud, Open WebUI, SQL Server, Entra ID, and MCP Streamable HTTP.

## Implemented
- Automatic Entra user registration on first authenticated request.
- GitHub account registration through the GitHub App OAuth web flow.
- SQL Server persistence with EF Core.
- Internal roles and repository grants, combined with a live GitHub permission check.
- MCP endpoint at `/mcp`.
- MCP tools to list authorized repositories and request commit/PR approvals.
- Mandatory explicit portal approval before commit or pull request execution.
- Approval payload SHA-256 binding, random single-use token, 15-minute expiry, optimistic row versioning, reauthorization before execution, and SQL audit records.
- GitHub Contents API commit operations and Pull Request API operations.
- IIS-ready ASP.NET Core application and health endpoint.

## Important design boundary
The AI-facing MCP tools can only create `Pending` approval requests. They cannot approve or execute GitHub mutations. Approval occurs in an authenticated MVC page, and execution is a separate authenticated POST. Protected branch rules and required GitHub reviewers remain required.

## Configure
1. Install .NET 10 SDK and the ASP.NET Core 10 Hosting Bundle on the IIS server.
2. Create an Entra app registration with callback `https://hq-githubmcp.bfsaul.net/signin-oidc` and logout callback.
3. Create a GitHub App in the enterprise organization. Enable user authorization. Set callback to `https://hq-githubmcp.bfsaul.net/github/callback`. Grant repository Metadata read, Contents read/write, and Pull requests read/write. Install it only on approved repositories.
4. Update nonsecret settings in `appsettings.json`.
5. Store secrets outside source control:
   `dotnet user-secrets set "GitHub:ClientSecret" "..." --project src/HQ.GitHubMCP`
   For IIS, prefer environment variables or an enterprise secret store.
6. Generate and review the migration script using `src/HQ.GitHubMCP/Migrations/README.md`.
7. Seed the first Administrator directly through a controlled SQL change by setting the trusted user's `Role` to `2`. Entra app roles can replace this bootstrap process.
8. Publish: `dotnet publish src/HQ.GitHubMCP -c Release -o publish`.
9. Create an IIS app pool with **No Managed Code**, point the site at `publish`, bind TLS, and grant the app-pool identity SQL access.
10. In Open WebUI, add a Streamable HTTP MCP connection to `https://hq-githubmcp.bfsaul.net/mcp` with an authentication method that propagates a valid user identity.

## Security before production
- Replace user-secrets with managed secret storage and persist ASP.NET Core Data Protection keys outside the web root, protected by certificate or DPAPI.
- Add GitHub webhook handling for revocation and installation changes.
- Add rate limiting, CSP/security headers, anti-automation controls, structured logging, and SIEM forwarding.
- Implement enterprise group-to-role synchronization rather than manual role changes.
- Add unit/integration tests, SAST, dependency scanning, secret scanning, and a deployment pipeline.
- The sample commits files individually through the Contents API. For atomic multi-file commits, replace it with the Git Data API tree/commit/ref sequence.
- Ensure IIS and any reverse proxy do not buffer or time out MCP streaming responses.

## Build
`dotnet restore HQ.GitHubMCP.slnx`
`dotnet build HQ.GitHubMCP.slnx -c Release`
