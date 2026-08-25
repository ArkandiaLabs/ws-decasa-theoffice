# Azure DevOps access — MCP server or `az` CLI

Two ways to reach Azure DevOps. Pick one at Phase 0, announce which, and use it for
the whole run. **Never mix them silently** — a half-MCP, half-CLI run makes a failure
impossible to diagnose.

## Choosing a path

1. **MCP** — the session exposes `mcp__azure-devops__*` tools. Preferred: structured
   input and output, no shell quoting, no org/project defaults to get wrong.
2. **CLI** — otherwise, if all three hold:
   - `command -v az` succeeds;
   - `az extension list --query "[?name=='azure-devops']" -o tsv` is non-empty
     (install with `az extension add --name azure-devops`);
   - `az account show` succeeds, i.e. you are logged in.
3. **Neither** — stop and report both setup options. Do not guess a work item's
   contents from its id.

## Operations

`az` commands below were verified against the `azure-devops` extension's own `--help`.
**MCP tool names were not** — only `wit_get_work_item` and
`wit_list_work_item_comments` are attested from prior use. On the MCP path, list the
server's actual tools before you call anything past those two, and use the real names
rather than the plausible ones guessed here.

| Operation | MCP | `az` CLI |
|---|---|---|
| Read a work item | `wit_get_work_item` | `az boards work-item show --id <id> -o json` |
| Read its comments | `wit_list_work_item_comments` | no first-class command — see **Comments** below |
| Set its state | discover (`wit_*`) | `az boards work-item update --id <id> --state "<State>"` |
| Comment on it | discover (`wit_*`) | `az boards work-item update --id <id> --discussion "<text>"` |
| Open a PR | discover (`repo_*` / `pr_*`) | `az repos pr create --title "<t>" --description "<body>" --source-branch <b> --target-branch <main> --work-items <id>` |
| Read a PR | discover | `az repos pr show --id <pr>` |
| List PRs for a branch | discover | `az repos pr list --source-branch <b> -o json` |
| Pipeline runs for a branch | discover | `az pipelines runs list --branch <b> --top 5 -o json` |
| One run's detail | discover | `az pipelines runs show --id <run>` |
| PR comment threads | discover | no first-class command — see **Threads** below |

Notes on the verified commands:

- `az boards work-item update --discussion "<text>"` **is** the comment-write path;
  there is no separate `comment add` command.
- `az repos pr create --work-items <id>` links the work item at creation time. There
  is also `az repos pr work-item add` to link one afterwards.
- **Never** pass `--auto-complete`, `--bypass-policy`, or `--squash` to
  `az repos pr create`, and never run `az repos pr update --status completed`. This
  workflow opens PRs; it does not merge them.
- Org and project are **never hardcoded**. Either the caller names them, or
  `az devops configure --defaults organization=<url> project=<name>` has set them, or
  auto-detection picks them up from an Azure DevOps git remote. If none applies, `az`
  errors with `--organization must be specified` — ask rather than inventing a value.

### Comments (reading) and Threads

The CLI has no `boards work-item comment list` or `repos pr thread list`. Both live
behind the REST API, reachable via `az devops invoke`. **Discover the resource name
before calling it** — the exact `--resource` string varies by API version, and a
wrong guess returns a confusing 404:

```bash
# what resources exist in an area
az devops invoke --org <url> --query "[?area=='wit'].resourceName" -o tsv
az devops invoke --org <url> --query "[?area=='git'].resourceName" -o tsv
```

Then call the one you found, e.g.:

```bash
az devops invoke --org <url> --area wit --resource <resource-from-discovery> \
  --route-parameters project=<project> workItemId=<id> --api-version 7.1-preview
```

If discovery fails or the call 404s, say so and fall back to the MCP path or to
reading the item in the browser. **Do not paste a REST invocation you have not seen
succeed** — a plausible-looking command that silently returns nothing is worse than
an honest "I can't read the comments on this path."

## Authentication

**MCP path.** The server is configured in the repo's `.mcp.json`. If a `wit_*` call
fails with *"Identity … has not been materialized, please use interactive login over
the browser first,"* the server is running with `--authentication azcli` and the Azure
CLI is handing it a guest token the organization won't resolve. Switch to
`--authentication pat`, which reads `PERSONAL_ACCESS_TOKEN` as
`base64("<anything>:<PAT>")` and needs the **Work Items (Read)** scope. A git-only PAT
will read repos fine and 401 on every `wit_*` call.

**CLI path.** `az login`, then either:

```bash
az devops configure --defaults organization=https://dev.azure.com/<org> project=<project>
```

or export a PAT for non-interactive use:

```bash
export AZURE_DEVOPS_EXT_PAT=<pat>   # needs Work Items (Read & Write) + Code (Read & Write)
```

A PAT scoped only to Code will authenticate `az repos` and fail on `az boards`.
