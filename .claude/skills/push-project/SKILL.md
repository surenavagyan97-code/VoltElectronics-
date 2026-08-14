---
name: push-project
description: How to commit and push changes for VoltElectronics, including which git identity to use. Use whenever asked to commit, push, or open a PR for this repo.
---

# Committing & pushing VoltElectronics

Repo: `https://github.com/surenavagyan97-code/VoltElectronics-.git` (remote `origin`), default
branch `main`.

## Which git identity to commit as

This repo has been worked on from multiple machines/accounts, so **don't assume the local
`git config user.email` is correct** — it has been wrong before (pointed at an unrelated work
identity while the actual GitHub account with push access is `surenavagyan97-code`, email
`surenavagyan97@gmail.com`).

Before committing:
1. Check whose credentials are actually usable for push on this machine:
   ```bash
   git credential-osxkeychain get <<< $'protocol=https\nhost=github.com\n'   # macOS
   ```
   (or check `gh auth status` if the GitHub CLI is set up). The `username` returned is the account
   that will actually push, regardless of the commit's author metadata.
2. If that username is `surenavagyan97-code`, use:
   ```
   name:  Suren Avagyan
   email: surenavagyan97@gmail.com
   ```
3. If it's a different account, or nothing is configured yet, **ask the user** which identity/email
   to use and whether auth needs setting up (`gh auth login`, a PAT, or an SSH key) before pushing —
   don't guess or silently fall back to whatever `git config` happens to say.
4. Set the identity for the commit only (never touch global/local git config):
   ```bash
   GIT_AUTHOR_NAME="Suren Avagyan" GIT_AUTHOR_EMAIL="surenavagyan97@gmail.com" \
   GIT_COMMITTER_NAME="Suren Avagyan" GIT_COMMITTER_EMAIL="surenavagyan97@gmail.com" \
   git commit -m "..."
   ```

## Standard flow

```bash
git status                     # review what changed
git diff                       # review the actual diff
git add <specific files>       # avoid blind `git add -A` if anything looks like it could hold secrets
git commit -m "..."            # imperative, explains *why*, matches this repo's existing log style
git push origin main
```

- Only commit/push when the user actually asks — don't do it proactively after making changes.
- Never `--force` push, never `--amend` a commit that's already been pushed, never skip hooks.
- `.env` is gitignored — never add it even if `git status` somehow shows it as untracked-but-wanted.
- Verify the build compiles before pushing (`dotnet build` for backend, `ng build` for frontend) —
  see the `run-project` skill for exact commands.
