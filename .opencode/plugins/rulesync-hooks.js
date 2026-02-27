export const RulesyncHooksPlugin = async ({ $ }) => {
  return {
    event: async ({ event }) => {
      if (event.type === "session.created") {
        await $`.rulesync/hooks/session-start-context.sh`
      }
    },
    "tool.execute.after": async (input) => {
      if (new RegExp("Write|Edit").test(input.tool)) {
        await $`.rulesync/hooks/post-edit-dotnet.sh`
      }
    },
  }
}
