# Target Surfaces

Use this matrix when deciding whether a behavior belongs in shared RuleSync content or in a target-specific block.

| Target | Runtime shape | Practical authoring rule |
| ------ | ------------- | ------------------------ |
| `claudecode` | Rules, ignore, MCP, commands, subagents, skills, hooks | Use canonical `allowed-tools`; this is the richest target surface. |
| `opencode` | Rules, MCP, commands, subagents, skills, hooks | Mark only tab-worthy agents as `primary`; `@mention` is the subagent path. |
| `copilot` | Rules, MCP, commands, subagents, skills, hooks | Use Copilot tool names, and remember `agent/runSubagent` is implicit. |
| `geminicli` | Rules, ignore, MCP, commands, skills, hooks | Keep hooks thin and portable; Gemini does not consume subagents directly. |
| `codexcli` | Rules, MCP, subagents, skills | Express read-only behavior with `sandbox_mode: "read-only"`; commands and hooks are not a native target surface here. |
| `antigravity` | Rules, commands, skills | Prefer concise, globally safe rules that work well when injected automatically; do not assume MCP or subagents. |
| `factorydroid` | Rules, MCP, hooks | Do not depend on imported skills, subagents, or commands at runtime; route through generated rules and hook text. |

## Command Compatibility Matrix

High-priority commands and their compatibility across targets:

| Command | claudecode | opencode | copilot | geminicli | codexcli | antigravity | factorydroid |
|---------|:----------:|:--------:|:-------:|:---------:|:--------:|:-----------:|:------------:|
| `dotnet-agent-harness-bootstrap` | ✓ | ✓ | ✓ | ✓ | ✓ | via rules | via rules |
| `dotnet-agent-harness-search` | ✓ | ✓ | ✓ | ✓ | ⚠️ read-only | via rules | via rules |
| `dotnet-agent-harness-graph` | ✓ | ✓ | ✓ | ✓ | ⚠️ read-only | via rules | via rules |
| `dotnet-agent-harness-test` | ✓ | ✓ | ✓ | ✓ | ✗ | via rules | via rules |
| `dotnet-agent-harness-compare` | ✓ | ✓ | ✓ | ✓ | ⚠️ read-only | via rules | via rules |
| `dotnet-agent-harness-incident` | ✓ | ✓ | ✓ | ✓ | ✗ | via rules | via rules |
| `dotnet-agent-harness-metadata` | ✓ | ✓ | ✓ | ✓ | ⚠️ read-only | via rules | via rules |
| `dotnet-agent-harness-recommend` | ✓ | ✓ | ✓ | ✓ | ⚠️ read-only | via rules | via rules |
| `dotnet-agent-harness-prepare-message` | ✓ | ✓ | ✓ | ✓ | ⚠️ read-only | via rules | via rules |
| `init-project` | ✓ | ✓ | ✓ | ✓ | ⚠️ read-only | ✓ trigger | via rules |
| `dotnet-slopwatch` | ✓ | ✓ | ✓ | ✓ | ⚠️ read-only | ✓ trigger | via rules |
| `deep-wiki-generate` | ✓ | ✓ | ✓ | ✓ | ✗ | via rules | via rules |
| `deep-wiki-build` | ✓ | ✓ | ✓ | ✓ | ✗ | via rules | via rules |
| `deep-wiki-page` | ✓ | ✓ | ✓ | ✓ | ⚠️ read-only | via rules | via rules |
| `deep-wiki-ask` | ✓ | ✓ | ✓ | ✓ | ⚠️ read-only | via rules | via rules |
| `deep-wiki-research` | ✓ | ✓ | ⚠️ WebSearch | ⚠️ WebSearch | ⚠️ read-only | via rules | via rules |
| `deep-wiki-catalogue` | ✓ | ✓ | ✓ | ✓ | ⚠️ read-only | via rules | via rules |
| `deep-wiki-onboard` | ✓ | ✓ | ✓ | ✓ | ⚠️ read-only | via rules | via rules |

**Legend:**
- ✓ Full support
- ⚠️ Limited support (see notes)
- ✗ Not supported
- via rules Available through generated rules/hooks only

## Hard Rules

- Start from the shared RuleSync shape, then add per-target blocks only for real runtime differences.
- Do not hand-edit generated target directories to patch a platform quirk; encode the difference back in `.rulesync/`.
- For `factorydroid`, treat rules-plus-hooks as the delivery mechanism even if source files also carry target metadata.
- When a target cannot consume a surface directly, move the important guidance up into `rules` or `hooks` rather than
  assuming skills or commands will be available.
