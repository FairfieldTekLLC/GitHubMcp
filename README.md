# HQ.GitHubMCP

Enterprise GitHub MCP server for AI-assisted software development.

HQ.GitHubMCP exposes a secured MCP endpoint (`/mcp`) that lets approved clients (for example Open WebUI) inspect repositories, modify code, manage pull requests/issues, interact with CI/CD workflows, and review security/governance data.

## What this project does

HQ.GitHubMCP combines five controls into one service:

1. **User authentication** via Microsoft Entra ID
2. **OAuth 2.1 authorization server** (OpenIddict) for MCP clients
3. **Repository authorization (RBAC)** via SQL Server grants (`Read`, `Write`, `Admin`)
4. **GitHub execution** through a GitHub App installation (Option A model)
5. **Auditable MCP tool surface** (61 tools) grouped by capability

### Architecture summary

- **Inbound:** OAuth-protected MCP calls to `/mcp`
- **Policy:** `McpBearer` requires authenticated bearer token with `MCP.Access` scope
- **Authorization path:** Entra identity -> SQL repository grant -> GitHub permission check
- **Outbound:** GitHub REST API calls against allowed repositories

## Key endpoints

- `POST /mcp` - MCP tool endpoint (requires bearer token + `MCP.Access`)
- `POST /oauth/register` - OAuth Dynamic Client Registration (DCR)
- `GET /oauth/authorize` / `POST /oauth/token` - OAuth authorization/token
- `GET /.well-known/oauth-protected-resource/mcp` - Protected resource metadata
- `GET /health` - health check
- `GET /github/callback` - GitHub OAuth callback for account linking

---

## Tool catalog (61 total)

> **Permission model:**
> - `Read` = non-mutating repo inspection
> - `Write` = file/branch/PR/issue/workflow/release mutations
> - `Admin` = security/governance-sensitive repository inspection

---

## 1) Core tools (`GitHubTools`) - 6

These are the foundational tools and approval-gated operations.

| Tool | Required permission | Description |
|---|---:|---|
| `ListRepositories` | Authenticated user | Returns repositories granted to the current user from SQL RBAC, including effective permission. |
| `CommitFiles` | `Write` | Directly commits one or more files to a branch in an authorized repository. |
| `CreatePullRequest` | `Write` | Directly creates a pull request in an authorized repository. |
| `CheckRepositoryPermission` | Read/Write/Admin (input-driven) | Verifies effective access by checking both SQL grant and GitHub-side authorization, without making changes. |
| `RequestCommitApproval` | Approval workflow | Creates a pending approval request for a commit (no code is committed until approved/executed). |
| `RequestPullRequestApproval` | Approval workflow | Creates a pending approval request for PR creation (no PR is created until approved/executed). |

---

## 2) Repository browse tools (`GitHubRepoBrowseTools`) - 12

All tools below require `Read`.

| Tool | Description |
|---|---|
| `ListRepositoryContents` | Lists files/folders at a path (root if path is empty), optionally at a specific branch/ref. |
| `ReadRepositoryFile` | Reads file metadata and content (GitHub returns base64 content). |
| `GetRepositoryTree` | Returns recursive tree (`git/trees?recursive=1`) for a branch/ref. |
| `SearchRepositoryCode` | Runs GitHub code search scoped to the target repository. |
| `ListRepositoryBranches` | Lists repository branches. |
| `GetRepositoryInfo` | Returns repository metadata/settings. |
| `GetRepositoryLanguages` | Returns language byte distribution. |
| `GetRepositoryReadme` | Returns README metadata/content. |
| `GetRepositoryContributors` | Lists contributors. |
| `GetCommitHistory` | Lists commits with optional filters (`branch`, `path`, `perPage`). |
| `GetCommitDetails` | Returns details for one commit (files/stats/metadata). |
| `CompareBranches` | Compares refs (`base...head`) and returns change summary. |

---

## 3) Repository write tools (`GitHubRepoWriteTools`) - 6

| Tool | Required permission | Description |
|---|---:|---|
| `WriteRepositoryFile` | `Write` | Create or update a file. If `fileSha` is omitted => create. If provided => update existing file. |
| `DeleteRepositoryFile` | `Write` | Deletes a file; requires current file SHA. |
| `CreateBranch` | `Write` | Creates a branch from source branch (or default branch). Internally resolves source HEAD SHA first. |
| `DeleteBranch` | `Write` | Deletes a non-default branch. Default branch deletion is blocked. |
| `GetFileDiff` | `Read` | Returns changed file data for a specific file between `baseRef` and `headRef`. |
| `GetCommitPatch` | `Write` | Returns commit patch data for cherry-pick/revert style workflows (REST has no direct revert/cherry-pick endpoint). |

---

## 4) Pull request + issue tools (`GitHubPullRequestIssueTools`) - 15

### Pull request tools

| Tool | Required permission | Description |
|---|---:|---|
| `ListPullRequests` | `Read` | Lists PRs by state (`open`, `closed`, `all`). |
| `GetPullRequestDetails` | `Read` | Returns PR `details`, `files`, or `reviews` based on `include`. |
| `UpdatePullRequest` | `Write` | Updates PR title/body. |
| `AddPullRequestComment` | `Write` | Adds PR comment (issues comments endpoint). |
| `ReviewPullRequest` | `Write` | Submits PR review event (`APPROVE`, `REQUEST_CHANGES`, `COMMENT`). |
| `MergePullRequest` | `Write` | Merges PR with method (`merge`, `squash`, `rebase`). |
| `ClosePullRequest` | `Write` | Closes PR without merge. |

### Issue tools

| Tool | Required permission | Description |
|---|---:|---|
| `ListIssues` | `Read` | Lists issues, or gets a single issue when `issueNumber` is provided. |
| `CreateIssue` | `Write` | Creates issue with optional labels/assignees. |
| `UpdateIssue` | `Write` | Updates issue title/body/state. |
| `AddIssueComment` | `Write` | Adds comment to issue. |
| `AssignIssue` | `Write` | Assigns one or more users (comma-separated input). |
| `ListLabels` | `Read` | Lists repository labels. |
| `AddIssueLabels` | `Write` | Adds labels to an issue. |
| `ListMilestones` | `Read` | Lists milestones. |

---

## 5) Actions + release tools (`GitHubActionsTools`) - 14

### GitHub Actions / CI

| Tool | Required permission | Description |
|---|---:|---|
| `ListWorkflows` | `Read` | Lists Actions workflows. |
| `GetWorkflow` | `Read` | Gets workflow by ID or file name (e.g., `build.yml`). |
| `ListWorkflowRuns` | `Read` | Lists runs, optionally scoped to a workflow and branch. |
| `GetWorkflowRun` | `Read` | Gets run status/conclusion. |
| `GetWorkflowRunJobs` | `Read` | Lists jobs/steps for run diagnostics. |
| `TriggerWorkflow` | `Write` | Triggers `workflow_dispatch` on target/default branch. |
| `RerunWorkflow` | `Write` | Reruns a workflow run. |
| `CancelWorkflow` | `Write` | Cancels an in-progress workflow run. |
| `ListWorkflowArtifacts` | `Read` | Lists workflow run artifacts. |
| `GetCommitStatus` | `Read` | Returns check runs / CI status for a commit SHA. |

### Releases / tags

| Tool | Required permission | Description |
|---|---:|---|
| `ListReleases` | `Read` | Lists releases, or returns a specific release when `tagName` is provided. |
| `CreateRelease` | `Write` | Creates release for a tag with optional notes generation. |
| `ListTags` | `Read` | Lists repository tags. |
| `GenerateReleaseNotes` | `Read` | Generates release notes for a tag (optionally from previous tag). |

---

## 6) Security + governance tools (`GitHubSecurityGovernanceTools`) - 8

### Security inspection

| Tool | Required permission | Description |
|---|---:|---|
| `ListSecurityAlerts` | `Admin` | Lists or retrieves security alerts (`dependabot`, `code-scanning`, `secret-scanning`). |
| `GetBranchProtection` | `Admin` | Returns branch protection configuration. |
| `ListRepositoryCollaborators` | `Admin` | Lists collaborators and roles. |

### Governance / self-service

| Tool | Required permission | Description |
|---|---:|---|
| `ListMyRepositoryGrants` | Authenticated user | Returns current user’s SQL repository grants. |
| `GetMyGitHubIdentity` | Authenticated user | Returns linked GitHub identity metadata (no secrets). |
| `ValidateRepositoryConnection` | `Read` | Verifies read connectivity/authorization to repository. |
| `GetMcpHealth` | Authenticated user | Returns service health indicators (auth/db/app mode). |
| `GetRepositoryActivity` | `Read` | Returns recent repository events. |

---

## Tool quick-reference (one-line input/output examples)

> Notes:
> - `repositoryId` is the internal GUID from `ListRepositories`.
> - Examples are minimal and abbreviated for readability.

### Core tools (`GitHubTools`)

| Tool | One-line input example | One-line output example |
|---|---|---|
| `ListRepositories` | `{}` | `[{"id":"b5...","owner":"B-F-Saul-Company","name":"HQ.GitHubMCP","permission":"Write"}]` |
| `CommitFiles` | `{"repositoryId":"b5...","branch":"feature/fix","message":"Fix auth","files":[{"path":"src/A.cs","content":"..."}]}` | `"https://ghe.com/B-F-Saul-Company/HQ.GitHubMCP/commit/abc123"` |
| `CreatePullRequest` | `{"repositoryId":"b5...","head":"feature/fix","base":"main","title":"Fix auth","body":"Details"}` | `"https://ghe.com/B-F-Saul-Company/HQ.GitHubMCP/pull/42"` |
| `CheckRepositoryPermission` | `{"repositoryId":"b5...","permission":"Write"}` | `{"authorized":true,"requiredPermission":"Write","message":"Both authorization checks succeeded."}` |
| `RequestCommitApproval` | `{"repositoryId":"b5...","branch":"main","message":"Hotfix","files":[{"path":"README.md","content":"..."}]}` | `{"id":"2a...","reviewUrl":"/Approvals/Review/2a...","message":"No commit has been created..."}` |
| `RequestPullRequestApproval` | `{"repositoryId":"b5...","head":"feature/x","base":"main","title":"PR title","body":"PR body"}` | `{"id":"9f...","reviewUrl":"/Approvals/Review/9f...","message":"No pull request has been created..."}` |

### Repository browse tools (`GitHubRepoBrowseTools`)

| Tool | One-line input example | One-line output example |
|---|---|---|
| `ListRepositoryContents` | `{"repositoryId":"b5...","path":"src","branch":"main"}` | `[{"name":"Program.cs","path":"src/Program.cs","type":"file"}]` |
| `ReadRepositoryFile` | `{"repositoryId":"b5...","path":"README.md","branch":"main"}` | `{"name":"README.md","sha":"e1...","content":"IyBIUS4uLg=="}` |
| `GetRepositoryTree` | `{"repositoryId":"b5...","branch":"main"}` | `{"tree":[{"path":"src/Program.cs","type":"blob"}]}` |
| `SearchRepositoryCode` | `{"repositoryId":"b5...","query":"RepositoryPermission"}` | `{"total_count":3,"items":[{"path":"Models/Entities.cs"}]}` |
| `ListRepositoryBranches` | `{"repositoryId":"b5..."}` | `[{"name":"main"},{"name":"feature/a"}]` |
| `GetRepositoryInfo` | `{"repositoryId":"b5..."}` | `{"full_name":"B-F-Saul-Company/HQ.GitHubMCP","default_branch":"main"}` |
| `GetRepositoryLanguages` | `{"repositoryId":"b5..."}` | `{"C#":152340,"JavaScript":8920}` |
| `GetRepositoryReadme` | `{"repositoryId":"b5..."}` | `{"name":"README.md","content":"IyBIUS4uLg=="}` |
| `GetRepositoryContributors` | `{"repositoryId":"b5..."}` | `[{"login":"alice","contributions":120}]` |
| `GetCommitHistory` | `{"repositoryId":"b5...","branch":"main","path":"src","perPage":20}` | `[{"sha":"abc123","commit":{"message":"Refactor"}}]` |
| `GetCommitDetails` | `{"repositoryId":"b5...","commitSha":"abc123"}` | `{"sha":"abc123","files":[{"filename":"src/A.cs","changes":12}]}` |
| `CompareBranches` | `{"repositoryId":"b5...","baseRef":"main","headRef":"feature/fix"}` | `{"status":"ahead","ahead_by":3,"files":[{"filename":"src/A.cs"}]}` |

### Repository write tools (`GitHubRepoWriteTools`)

| Tool | One-line input example | One-line output example |
|---|---|---|
| `WriteRepositoryFile` | `{"repositoryId":"b5...","path":"docs/notes.md","content":"Hello","commitMessage":"Add notes","fileSha":null,"branch":"feature/x"}` | `{"content":{"path":"docs/notes.md"},"commit":{"sha":"d4..."}}` |
| `DeleteRepositoryFile` | `{"repositoryId":"b5...","path":"docs/old.md","commitMessage":"Remove old doc","fileSha":"aa...","branch":"main"}` | `{"commit":{"sha":"ef..."}}` |
| `CreateBranch` | `{"repositoryId":"b5...","newBranchName":"feature/new-api","sourceBranch":"main"}` | `{"ref":"refs/heads/feature/new-api","object":{"sha":"ab..."}}` |
| `DeleteBranch` | `{"repositoryId":"b5...","branchName":"feature/old"}` | `{"success":true,"statusCode":204}` |
| `GetFileDiff` | `{"repositoryId":"b5...","baseRef":"main","headRef":"feature/fix","path":"src/A.cs"}` | `{"filename":"src/A.cs","status":"modified","patch":"@@ ..."}` |
| `GetCommitPatch` | `{"repositoryId":"b5...","commitSha":"abc123"}` | `{"sha":"abc123","files":[{"filename":"src/A.cs","patch":"@@ ..."}]}` |

### Pull request + issue tools (`GitHubPullRequestIssueTools`)

| Tool | One-line input example | One-line output example |
|---|---|---|
| `ListPullRequests` | `{"repositoryId":"b5...","state":"open"}` | `[{"number":42,"title":"Fix auth","state":"open"}]` |
| `GetPullRequestDetails` | `{"repositoryId":"b5...","pullNumber":42,"include":"details"}` | `{"number":42,"head":{"ref":"feature/fix"},"base":{"ref":"main"}}` |
| `UpdatePullRequest` | `{"repositoryId":"b5...","pullNumber":42,"title":"Fix auth flow","body":"Updated"}` | `{"number":42,"title":"Fix auth flow"}` |
| `AddPullRequestComment` | `{"repositoryId":"b5...","pullNumber":42,"comment":"Please add tests"}` | `{"id":12345,"body":"Please add tests"}` |
| `ReviewPullRequest` | `{"repositoryId":"b5...","pullNumber":42,"reviewEvent":"APPROVE","body":"Looks good"}` | `{"id":777,"state":"APPROVED"}` |
| `MergePullRequest` | `{"repositoryId":"b5...","pullNumber":42,"mergeMethod":"squash","commitMessage":"Merge PR #42"}` | `{"merged":true,"sha":"9c...","message":"Pull Request successfully merged"}` |
| `ClosePullRequest` | `{"repositoryId":"b5...","pullNumber":42}` | `{"number":42,"state":"closed"}` |
| `ListIssues` | `{"repositoryId":"b5...","issueNumber":null,"state":"open","labels":"bug"}` | `[{"number":18,"title":"OAuth bug","state":"open"}]` |
| `CreateIssue` | `{"repositoryId":"b5...","title":"Bug report","body":"Steps...","labels":"bug,high","assignees":"alice"}` | `{"number":19,"title":"Bug report","state":"open"}` |
| `UpdateIssue` | `{"repositoryId":"b5...","issueNumber":19,"title":"Bug report (updated)","body":"More detail","state":"open"}` | `{"number":19,"title":"Bug report (updated)"}` |
| `AddIssueComment` | `{"repositoryId":"b5...","issueNumber":19,"comment":"Investigating"}` | `{"id":555,"body":"Investigating"}` |
| `AssignIssue` | `{"repositoryId":"b5...","issueNumber":19,"assignees":"alice,bob"}` | `{"number":19,"assignees":[{"login":"alice"},{"login":"bob"}]}` |
| `ListLabels` | `{"repositoryId":"b5..."}` | `[{"name":"bug"},{"name":"enhancement"}]` |
| `AddIssueLabels` | `{"repositoryId":"b5...","issueNumber":19,"labels":"bug,triaged"}` | `[{"name":"bug"},{"name":"triaged"}]` |
| `ListMilestones` | `{"repositoryId":"b5..."}` | `[{"number":1,"title":"v1.0","state":"open"}]` |

### Actions + release tools (`GitHubActionsTools`)

| Tool | One-line input example | One-line output example |
|---|---|---|
| `ListWorkflows` | `{"repositoryId":"b5..."}` | `{"workflows":[{"id":123,"name":"CI"}]}` |
| `GetWorkflow` | `{"repositoryId":"b5...","workflowIdOrFileName":"build.yml"}` | `{"id":123,"name":"CI","path":".github/workflows/build.yml"}` |
| `ListWorkflowRuns` | `{"repositoryId":"b5...","workflowIdOrFileName":"build.yml","branch":"main"}` | `{"workflow_runs":[{"id":1001,"status":"completed"}]}` |
| `GetWorkflowRun` | `{"repositoryId":"b5...","runId":1001}` | `{"id":1001,"status":"completed","conclusion":"success"}` |
| `GetWorkflowRunJobs` | `{"repositoryId":"b5...","runId":1001}` | `{"jobs":[{"id":1,"name":"build","conclusion":"success"}]}` |
| `TriggerWorkflow` | `{"repositoryId":"b5...","workflowIdOrFileName":"build.yml","branch":"main"}` | `{"success":true,"statusCode":204}` |
| `RerunWorkflow` | `{"repositoryId":"b5...","runId":1001}` | `{"success":true,"statusCode":201}` |
| `CancelWorkflow` | `{"repositoryId":"b5...","runId":1002}` | `{"success":true,"statusCode":202}` |
| `ListWorkflowArtifacts` | `{"repositoryId":"b5...","runId":1001}` | `{"artifacts":[{"id":8,"name":"drop.zip"}]}` |
| `GetCommitStatus` | `{"repositoryId":"b5...","commitSha":"abc123"}` | `{"check_runs":[{"name":"CI","conclusion":"success"}]}` |
| `ListReleases` | `{"repositoryId":"b5...","tagName":null}` | `[{"id":1,"tag_name":"v1.0.0"}]` |
| `CreateRelease` | `{"repositoryId":"b5...","tagName":"v1.1.0","name":"v1.1.0","body":"Notes","draft":false,"prerelease":false,"generateReleaseNotes":true}` | `{"id":2,"html_url":"https://.../releases/tag/v1.1.0"}` |
| `ListTags` | `{"repositoryId":"b5..."}` | `[{"name":"v1.1.0","commit":{"sha":"ab..."}}]` |
| `GenerateReleaseNotes` | `{"repositoryId":"b5...","tagName":"v1.1.0","previousTagName":"v1.0.0"}` | `{"name":"v1.1.0","body":"## What's Changed ..."}` |

### Security + governance tools (`GitHubSecurityGovernanceTools`)

| Tool | One-line input example | One-line output example |
|---|---|---|
| `ListSecurityAlerts` | `{"repositoryId":"b5...","alertType":"dependabot","state":"open","alertNumber":null}` | `[{"number":11,"state":"open","security_advisory":{"summary":"..."}}]` |
| `GetBranchProtection` | `{"repositoryId":"b5...","branch":"main"}` | `{"required_pull_request_reviews":{"required_approving_review_count":1}}` |
| `ListRepositoryCollaborators` | `{"repositoryId":"b5..."}` | `[{"login":"alice","permissions":{"push":true}}]` |
| `ListMyRepositoryGrants` | `{}` | `[{"repositoryId":"b5...","owner":"B-F-Saul-Company","name":"HQ.GitHubMCP","permission":"Admin"}]` |
| `GetMyGitHubIdentity` | `{}` | `{"connected":true,"identity":{"login":"vince","gitHubUserId":12345}}` |
| `ValidateRepositoryConnection` | `{"repositoryId":"b5..."}` | `{"full_name":"B-F-Saul-Company/HQ.GitHubMCP","private":true}` |
| `GetMcpHealth` | `{}` | `{"healthy":true,"authenticated":true,"gitHubAppMode":true}` |
| `GetRepositoryActivity` | `{"repositoryId":"b5..."}` | `[{"type":"PushEvent","actor":{"login":"alice"}}]` |

---

## Security and authorization behavior

- Every repository-scoped tool resolves repository by internal `repositoryId` and checks:
  1. repository exists and is enabled
  2. caller has required SQL grant
  3. caller/app has required GitHub authorization
- Calls are executed through a shared executor (`GitHubToolExecutor`) with consistent error handling and response shaping.
- Non-success GitHub API responses are returned as structured error payloads (`Error`, `StatusCode`, `Message`).

## Operational notes

- OAuth certificates are loaded by thumbprint from `LocalMachine/My` and must include private keys.
- MCP tokens require `MCP.Access` scope.
- Dynamic registration and token flow support refresh tokens (`offline_access`).
- Tool surface is intentionally grouped to keep model/tool selection manageable.

## Project structure

- `Program.cs` - service wiring, auth, OpenIddict, MCP registration
- `Services/` - GitHub integration, RBAC gate, approvals, auditing
- `Tools/` - MCP tool classes (all callable tools)
- `Controllers/` - account, OAuth, approval/repository UI endpoints
- `Data/` + `Migrations/` - EF Core DB context and schema

## Development notes

- Build: `dotnet build`
- MCP endpoint: `/mcp`
- Health endpoint: `/health`

For environment and deployment instructions, see `install.me` in this project.

---

## Appendix: Copy/paste JSON payloads (top 10 common tasks)

> Replace sample values before use:
> - `repositoryId` must be a real GUID returned by `ListRepositories`
> - branch names, SHAs, issue/PR numbers, and workflow IDs must exist in your repository
> - Response schemas below are QA-focused shape checks (not strict JSON Schema drafts)

### 1) List repositories available to the current user

Tool: `ListRepositories`

```json
{}
```

Expected response schema:

```json
{
  "type": "array",
  "items": {
    "type": "object",
    "required": ["id", "owner", "name", "defaultBranch", "permission"],
    "properties": {
      "id": { "type": "string", "format": "uuid" },
      "owner": { "type": "string" },
      "name": { "type": "string" },
      "defaultBranch": { "type": "string" },
      "permission": { "type": "string", "enum": ["Read", "Write", "Admin"] }
    }
  }
}
```

### 2) Check whether the current user has Write access

Tool: `CheckRepositoryPermission`

```json
{
  "repositoryId": "6f63c140-8a53-4bd1-95cc-a2fd9e97f2a7",
  "permission": "Write"
}
```

Expected response schema:

```json
{
  "type": "object",
  "required": ["authorized"],
  "properties": {
    "authorized": { "type": "boolean" },
    "repository": { "type": "string" },
    "repositoryId": { "type": "string", "format": "uuid" },
    "requiredPermission": { "type": "string" },
    "sqlGrantVerified": { "type": "boolean" },
    "gitHubPermissionVerified": { "type": "boolean" },
    "message": { "type": "string" },
    "reason": { "type": "string" }
  }
}
```

### 3) Read a file from the default branch

Tool: `ReadRepositoryFile`

```json
{
  "repositoryId": "6f63c140-8a53-4bd1-95cc-a2fd9e97f2a7",
  "path": "README.md",
  "branch": "main"
}
```

Expected response schema:

```json
{
  "type": "object",
  "required": ["name", "path", "sha", "type"],
  "properties": {
    "name": { "type": "string" },
    "path": { "type": "string" },
    "sha": { "type": "string" },
    "type": { "type": "string" },
    "size": { "type": "integer" },
    "encoding": { "type": "string" },
    "content": { "type": "string", "description": "base64 encoded" }
  }
}
```

### 4) Create a new branch from `main`

Tool: `CreateBranch`

```json
{
  "repositoryId": "6f63c140-8a53-4bd1-95cc-a2fd9e97f2a7",
  "newBranchName": "feature/add-oauth-logging",
  "sourceBranch": "main"
}
```

Expected response schema:

```json
{
  "type": "object",
  "required": ["ref", "object"],
  "properties": {
    "ref": { "type": "string" },
    "object": {
      "type": "object",
      "required": ["type", "sha"],
      "properties": {
        "type": { "type": "string" },
        "sha": { "type": "string" }
      }
    }
  }
}
```

### 5) Create a new file (no `fileSha`)

Tool: `WriteRepositoryFile`

```json
{
  "repositoryId": "6f63c140-8a53-4bd1-95cc-a2fd9e97f2a7",
  "path": "docs/operational-checklist.md",
  "content": "# Operational Checklist\n\n- Verify health endpoint\n- Verify OAuth discovery\n- Verify RBAC grants\n",
  "commitMessage": "Add operational checklist documentation",
  "fileSha": null,
  "branch": "feature/add-oauth-logging"
}
```

Expected response schema:

```json
{
  "type": "object",
  "required": ["content", "commit"],
  "properties": {
    "content": {
      "type": "object",
      "required": ["name", "path", "sha"],
      "properties": {
        "name": { "type": "string" },
        "path": { "type": "string" },
        "sha": { "type": "string" }
      }
    },
    "commit": {
      "type": "object",
      "required": ["sha", "message"],
      "properties": {
        "sha": { "type": "string" },
        "message": { "type": "string" }
      }
    }
  }
}
```

### 6) Update an existing file (include current `fileSha`)

Tool: `WriteRepositoryFile`

```json
{
  "repositoryId": "6f63c140-8a53-4bd1-95cc-a2fd9e97f2a7",
  "path": "README.md",
  "content": "# HQ.GitHubMCP\n\nUpdated quick start section.\n",
  "commitMessage": "Update README quick start text",
  "fileSha": "e69de29bb2d1d6434b8b29ae775ad8c2e48c5391",
  "branch": "feature/add-oauth-logging"
}
```

Expected response schema:

```json
{
  "type": "object",
  "required": ["content", "commit"],
  "properties": {
    "content": {
      "type": "object",
      "required": ["path", "sha"],
      "properties": {
        "path": { "type": "string" },
        "sha": { "type": "string" }
      }
    },
    "commit": {
      "type": "object",
      "required": ["sha", "message"],
      "properties": {
        "sha": { "type": "string" },
        "message": { "type": "string" }
      }
    }
  }
}
```

### 7) Create a pull request

Tool: `CreatePullRequest`

```json
{
  "repositoryId": "6f63c140-8a53-4bd1-95cc-a2fd9e97f2a7",
  "head": "feature/add-oauth-logging",
  "base": "main",
  "title": "Improve OAuth diagnostics and documentation",
  "body": "## Summary\nAdds targeted OAuth diagnostics and updates documentation.\n\n## Validation\n- Build succeeds\n- Health endpoint verified\n- OAuth metadata endpoint verified"
}
```

Expected response schema:

```json
{
  "type": "string",
  "description": "HTML URL of created pull request"
}
```

### 8) Trigger a GitHub Actions workflow

Tool: `TriggerWorkflow`

```json
{
  "repositoryId": "6f63c140-8a53-4bd1-95cc-a2fd9e97f2a7",
  "workflowIdOrFileName": "build.yml",
  "branch": "main"
}
```

Expected response schema:

```json
{
  "oneOf": [
    {
      "type": "object",
      "required": ["success", "statusCode"],
      "properties": {
        "success": { "type": "boolean" },
        "statusCode": { "type": "integer" }
      }
    },
    {
      "type": "object",
      "required": ["error", "statusCode", "message"],
      "properties": {
        "error": { "type": "boolean" },
        "statusCode": { "type": "integer" },
        "message": { "type": "string" }
      }
    }
  ]
}
```

### 9) Open an issue with labels and assignees

Tool: `CreateIssue`

```json
{
  "repositoryId": "6f63c140-8a53-4bd1-95cc-a2fd9e97f2a7",
  "title": "OAuth callback occasionally returns access_denied",
  "body": "## Steps to reproduce\n1. Start OAuth flow\n2. Sign in with Entra\n3. Observe callback error\n\n## Expected\nToken exchange succeeds.",
  "labels": "bug,oauth,high-priority",
  "assignees": "vince,platform-team"
}
```

Expected response schema:

```json
{
  "type": "object",
  "required": ["number", "title", "state"],
  "properties": {
    "number": { "type": "integer" },
    "title": { "type": "string" },
    "state": { "type": "string" },
    "labels": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "name": { "type": "string" }
        }
      }
    },
    "assignees": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "login": { "type": "string" }
        }
      }
    }
  }
}
```

### 10) Retrieve open Dependabot alerts (Admin)

Tool: `ListSecurityAlerts`

```json
{
  "repositoryId": "6f63c140-8a53-4bd1-95cc-a2fd9e97f2a7",
  "alertType": "dependabot",
  "state": "open",
  "alertNumber": null
}
```

Expected response schema:

```json
{
  "oneOf": [
    {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["number", "state"],
        "properties": {
          "number": { "type": "integer" },
          "state": { "type": "string" },
          "dependency": { "type": "object" },
          "security_advisory": { "type": "object" }
        }
      }
    },
    {
      "type": "object",
      "required": ["number", "state"],
      "properties": {
        "number": { "type": "integer" },
        "state": { "type": "string" },
        "dependency": { "type": "object" },
        "security_advisory": { "type": "object" }
      }
    }
  ]
}
```
