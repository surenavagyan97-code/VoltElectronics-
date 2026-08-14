# Working preferences

- Do not launch/run the app (dev servers, `ng serve`, backend, docker-compose, browser automation, etc.) unless the user explicitly asks for it in that turn. Build/typecheck to verify changes compile, but stop there.
- See `.claude/skills/run-project/` for exact run commands (fast local-dev path vs. full Docker path) and `.claude/skills/push-project/` for how to commit/push, including which git identity to use — this repo has been pushed from multiple accounts.
