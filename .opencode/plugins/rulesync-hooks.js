export const RulesyncHooksPlugin = async ({ $ }) => {
  return {
    event: async ({ event }) => {
      if (event.type === "session.created") {
        await $`# Inline session-start-context.sh for OpenCode: Detect .NET project. set -uo pipefail; SLN_COUNT="$(find . -maxdepth 3 \\( -name '*.sln' -o -name '*.slnx' \\) 2>/dev/null | wc -l | tr -d ' ')" || SLN_COUNT=0; CSPROJ_COUNT="$(find . -maxdepth 3 -name '*.csproj' 2>/dev/null | wc -l | tr -d ' ')" || CSPROJ_COUNT=0; HAS_GLOBAL_JSON=false; [ -f "global.json" ] && HAS_GLOBAL_JSON=true; TFM=""; FIRST_CSPROJ="$(find . -maxdepth 3 -name '*.csproj' -print -quit 2>/dev/null)" || true; [ -n "$FIRST_CSPROJ" ] && TFM="$(sed -n 's/.*<TargetFramework[s]\\{0,1\\}>\\([^<;]*\\).*/\\1/p' "$FIRST_CSPROJ" 2>/dev/null | head -1)" || true; CONTEXT=""; if [ "$SLN_COUNT" -gt 0 ] || [ "$CSPROJ_COUNT" -gt 0 ] || [ "$HAS_GLOBAL_JSON" = true ]; then CONTEXT="Mandatory first action for every task: invoke [skill:dotnet-advisor]. Do not plan, reason, design, or implement until it has been invoked, then follow its routing to load additional skills and apply their standards."; PROJECT_CONTEXT="This is a .NET project"; [ -n "$TFM" ] && PROJECT_CONTEXT="This is a .NET project ($TFM)"; [ "$CSPROJ_COUNT" -gt 0 ] && PROJECT_CONTEXT="$PROJECT_CONTEXT with $CSPROJ_COUNT project(s)"; [ "$SLN_COUNT" -gt 0 ] && PROJECT_CONTEXT="$PROJECT_CONTEXT in $SLN_COUNT solution(s)"; PROJECT_CONTEXT="$PROJECT_CONTEXT."; CONTEXT="$CONTEXT $PROJECT_CONTEXT"; fi; if command -v jq >/dev/null 2>&1; then jq -Rn --arg additionalContext "$CONTEXT" '{additionalContext: $additionalContext}'; else ESCAPED_CONTEXT="$(printf '%s' "$CONTEXT" | sed 's/\\\\/\\\\\\\\/g; s/"/\\\\"/g; s/\\r/\\\\r/g; s/\\t/\\\\t/g')"; printf '{"additionalContext":"%s"}\\n' "$ESCAPED_CONTEXT"; fi; exit 0`
      }
    },
    "tool.execute.after": async (input) => {
      if (new RegExp("Write|Edit").test(input.tool)) {
        await $`bash .rulesync/hooks/dotnet-agent-harness-post-edit-roslyn.sh`
      }
      if (new RegExp("Write|Edit").test(input.tool)) {
        await $`# Inline post-edit-dotnet.sh: Handle file edits with dotnet-specific actions. set -uo pipefail; [ -t 0 ] && INPUT="" || INPUT="$(cat)"; if ! command -v jq >/dev/null 2>&1 || [ -z "$INPUT" ]; then exit 0; fi; FILE_PATH="$(printf '%s' "$INPUT" | jq -r '.tool_input.file_path // .file_path // .filePath // empty')"; [ -z "$FILE_PATH" ] && exit 0; FILENAME="$(basename "$FILE_PATH")"; emit_message() { jq -n --arg msg "$1" '{ systemMessage: $msg }'; }; case "$FILE_PATH" in *Tests.cs|*Test.cs) TEST_CLASS="\${FILENAME%.cs}"; emit_message "Test file modified: $FILENAME. Consider running: dotnet test --filter $TEST_CLASS" ;; *.cs) if command -v dotnet >/dev/null 2>&1; then dotnet format --include "$FILE_PATH" --verbosity quiet >/dev/null 2>&1 || true; emit_message "dotnet format applied to $FILENAME"; else emit_message "dotnet not found in PATH -- skipping format. Install .NET SDK to enable auto-formatting."; fi ;; *.csproj) emit_message "Project file modified: $FILENAME. Consider running: dotnet restore" ;; *.xaml) if command -v xmllint >/dev/null 2>&1; then if xmllint --noout "$FILE_PATH" 2>/dev/null; then emit_message "XAML validation: $FILENAME is well-formed"; else emit_message "XAML validation: $FILENAME has XML errors. Check for unclosed tags or invalid syntax."; fi; else emit_message "No XML validator found (xmllint) -- skipping XAML validation for $FILENAME"; fi ;; *) exit 0 ;; esac; exit 0`
      }
      if (new RegExp("Write|Edit").test(input.tool)) {
        await $`bash .rulesync/hooks/dotnet-agent-harness-slopwatch.sh`
      }
      if (new RegExp("Bash").test(input.tool)) {
        await $`bash .rulesync/hooks/dotnet-agent-harness-inline-error-recovery.sh`
      }
    },
  }
}
