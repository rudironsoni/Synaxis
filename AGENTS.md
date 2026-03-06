Please also reference the following rules as needed. The list below is provided in TOON format, and `@` stands for the project root directory.

rules[2]:
  - path: @.agents/memories/10-conventions.md
    description: "dotnet-agent-harness authoring conventions for skills, subagents, commands, and hooks"
    applyTo[1]: .rulesync/**/*
  - path: @.agents/memories/20-workflow.md
    description: Workflow for RuleSync-based multi-agent generation
    applyTo[1]: **/*

# Additional Conventions Beyond the Built-in Functions

As this project's AI coding tool, you must follow the additional conventions below, in addition to the built-in functions.

# dotnet-agent-harness

Comprehensive .NET development guidance for modern C#, ASP.NET Core, MAUI, Blazor, and cloud-native apps.

## Overview

This toolkit provides:

- 189 skills
- 15 specialist agents/subagents
- shared RuleSync rules, commands, hooks, and MCP config

Compatible targets include:

- Claude Code
- GitHub Copilot CLI
- OpenCode
- Codex CLI
- Gemini CLI
- Antigravity
- Factory Droid

## Recommended install

Prefer the local runtime tool when this repository is installed into another .NET codebase:

```bash
dotnet new tool-manifest
dotnet tool install Rudironsoni.DotNetAgentHarness
dotnet agent-harness bootstrap --targets claudecode,opencode,codexcli,geminicli,copilot,antigravity,factorydroid --run-rulesync
```

RuleSync-only installation still works:

```bash
rulesync fetch rudironsoni/dotnet-agent-harness:.rulesync
rulesync generate --targets "claudecode,codexcli,opencode,geminicli,antigravity,copilot,factorydroid" --features "*"
```

If you use declarative sources:

```jsonc
{
  "sources": [{ "source": "rudironsoni/dotnet-agent-harness", "path": ".rulesync" }],
}
```

```bash
rulesync install
rulesync generate --targets "claudecode,codexcli,opencode,geminicli,antigravity,copilot,factorydroid" --features "*"
```

## OpenCode behavior

- Tab cycles **primary** agents only.
- `@mention` invokes subagents.
- `dotnet-architect` is configured as a primary OpenCode agent in this toolkit so it can appear in Tab rotation.

## Troubleshooting

If RuleSync reports `Multiple root rulesync rules found`, ensure only one root overview rule exists in
`.rulesync/rules/`.

If `dotnet-agent-harness:*` commands are available, prefer executing the local runtime command
(`dotnet agent-harness ...`) instead of manually reproducing catalog, prompt, incident, or graph logic from source
files.

## Contributing

Edit source files in `.rulesync/` and validate with `npm run ci:rulesync`.

## License

MIT License. See `LICENSE`.
