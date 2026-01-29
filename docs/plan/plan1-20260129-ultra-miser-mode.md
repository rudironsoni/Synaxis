# Enhanced "Ultra Miser" Strategy Plan

## Overview
We will configure Synaxis to chain multiple free providers. The Smart Router will automatically prioritize them based on the new `IsFree` flag.

## Provider Tiering

| Tier | Role             | Provider      | Key Models                         | Limits/Notes                                      |
| ---- | ---------------- | ------------- | ---------------------------------- | ------------------------------------------------- |
| **0**    | **SOTA Speedster**   | **SambaNova**     | `Llama-3.1-405B`                     | **Free.** 405B at 100+ t/s! (Rate limit: ~5 RPM)      |
| **1**    | **Workhorse (Free)** | **SiliconFlow**   | `DeepSeek-V3`, `DeepSeek-R1`, `Qwen-2.5` | **Free.** "Permanent free" for these specific models. |
| **1**    | **Workhorse (Free)** | **Z.ai**          | `glm-4-flash`                        | **Free.** Fast, reliable basic tasks.                 |
| **2**    | **High IQ (Daily)**  | **GitHub Models** | `GPT-4o`, `Phi-3.5`                    | **Free.** Rate limited, good for hard logic.          |
| **2**    | **High IQ (Daily)**  | **Google AI**     | `Gemini 1.5 Pro`                     | **Free.** High rate limits, massive context.          |
| **3**    | **Backup**           | **Hyperbolic**    | `DeepSeek-V3`, `Llama-3.1-405B`        | **Free.** Good backup for Samba/Silicon.              |

## Implementation Plan

### 1. Code Modifications (Infrastructure)
Enable the gateway to handle these "Static Free" providers without database lookups.

*   **`src/InferenceGateway/Application/Configuration/SynaxisConfiguration.cs`**
    *   Update `ProviderConfig`:
        *   Add `public bool IsFree { get; set; }` (Default: `false`).
        *   Add `public Dictionary<string, string>? CustomHeaders { get; set; }` (Needed for some providers like GitHub Models if they require specific headers).

*   **`src/InferenceGateway/Application/Routing/EnrichedCandidate.cs`**
    *   Update `IsFree` logic: `public bool IsFree => Config.IsFree || (Cost?.FreeTier ?? false);`

*   **`src/InferenceGateway/Infrastructure/Extensions/InfrastructureExtensions.cs`**
    *   Update `AddSynaxisInfrastructure`: In the `OpenAI` case switch, pass `config.CustomHeaders` to the client registration method.

### 2. Configuration Updates (`appsettings.json`)
Add the new providers.

```json
"SiliconFlow": {
  "Enabled": true,
  "Type": "OpenAI",
  "Endpoint": "https://api.siliconflow.cn/v1",
  "Key": "REPLACE_WITH_SILICONFLOW_KEY",
  "IsFree": true,
  "Models": [ "deepseek-ai/DeepSeek-V3", "deepseek-ai/DeepSeek-R1", "Qwen/Qwen2.5-7B-Instruct" ]
},
"SambaNova": {
  "Enabled": true,
  "Type": "OpenAI",
  "Endpoint": "https://api.sambanova.ai/v1",
  "Key": "REPLACE_WITH_SAMBANOVA_KEY",
  "IsFree": true,
  "Models": [ "Meta-Llama-3.1-405B-Instruct", "Meta-Llama-3.1-70B-Instruct" ]
},
"Zai": {
  "Enabled": true,
  "Type": "OpenAI",
  "Endpoint": "https://open.bigmodel.cn/api/paas/v4",
  "Key": "REPLACE_WITH_ZAI_KEY",
  "IsFree": true,
  "Models": [ "glm-4-flash" ]
},
"GitHubModels": {
  "Enabled": true,
  "Type": "OpenAI",
  "Endpoint": "https://models.inference.ai.azure.com",
  "Key": "REPLACE_WITH_GITHUB_PAT",
  "IsFree": true,
  "Models": [ "gpt-4o", "Llama-3.1-405B-Instruct", "Phi-3.5-mini-instruct" ]
},
"Hyperbolic": {
  "Enabled": true,
  "Type": "OpenAI",
  "Endpoint": "https://api.hyperbolic.xyz/v1",
  "Key": "REPLACE_WITH_HYPERBOLIC_KEY",
  "IsFree": true,
  "Models": [ "meta-llama/Meta-Llama-3.1-405B-Instruct", "deepseek-ai/DeepSeek-V3" ]
}
```

### 3. Canonical Aliases
Create aggregations so you can just ask for "smart" or "fast" and get the best free option.

*   `miser-intelligence`: `[ "sambanova/llama-405b", "github/gpt-4o", "hyperbolic/llama-405b" ]`
*   `miser-fast`: `[ "siliconflow/deepseek-v3", "zai/glm-4-flash", "groq/llama-8b" ]`
*   `miser-coding`: `[ "siliconflow/qwen-2.5", "github/gpt-4o" ]`
