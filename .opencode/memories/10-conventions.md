# Authoring Conventions

- Keep frontmatter compatible across targets. Prefer shared fields first (`name`, `description`, `targets`, `version`, `tags`).
- Add tool-specific fields only when required (`claudecode`, `opencode`, `codexcli`, `copilot`, `geminicli`).
- Use `version: "0.0.1"` for newly ported content unless a higher semantic version is intentionally introduced.
- Use ASCII in shell scripts and docs unless a source file already requires Unicode.
- Keep hook scripts advisory-only and return success to avoid blocking user workflows.
- Keep commands deterministic; avoid destructive actions unless explicitly requested.
