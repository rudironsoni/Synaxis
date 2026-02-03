# Configuration Guide

Synaxis is configured primarily through `src/InferenceGateway/WebApi/appsettings.json` (and `appsettings.Development.json` for dev overrides). This file defines which providers are active, their API keys, and the model routing configuration.

## Structure

The configuration is nested under `Synaxis:InferenceGateway`.

```json
{
  "Synaxis": {
    "InferenceGateway": {
      "Providers": {
        "ProviderName": {
          "Enabled": true,
          "Type": "ProviderType",
          "Key": "API_KEY",
          "Tier": 1,
          "Models": [ "model-id-1", "model-id-2" ]
        }
      },
      "CanonicalModels": [
        {
          "Id": "provider/model",
          "Provider": "ProviderName",
          "ModelPath": "model-id-1"
        }
      ],
      "Aliases": {
        "default": {
          "Candidates": [ "provider/model" ]
        }
      }
    }
  }
}
```

### Properties

| Property | Description | Required |
| :--- | :--- | :--- |
| `Type` | The type of the provider. See [Supported Providers](#supported-providers). | Yes |
| `Key` | The API Key for the provider. | Yes (except Pollinations) |
| `Enabled` | Whether this provider is active. | Yes |
| `Tier` | Routing priority. Lower numbers are tried first (e.g., Tier 1 before Tier 2). | Yes |
| `Models` | An array of model IDs that this provider handles. Incoming requests matching these IDs will be routed here. Use `*` for catch-all. | Yes |
| `AccountId` | The Account ID (Specific to Cloudflare). | Only for Cloudflare |
| `ProjectId` | Project ID (Specific to Antigravity). | Only for Antigravity |
| `Endpoint` | Override the default API endpoint URL. | No |

---

## Supported Providers

Based on the codebase (`InfrastructureExtensions.cs`), the following types are supported:

*   `OpenAI` (Generic/Official)
*   `Groq`
*   `Cohere`
*   `Cloudflare` (Workers AI)
*   `Gemini` (Google)
*   `OpenRouter`
*   `Nvidia`
*   `HuggingFace`
*   `Pollinations` (Free, no key required)
*   `Antigravity`

---

## Examples

### 1. Groq (Tier 1)
High-speed inference provider.

```json
"Groq": {
  "Type": "Groq",
  "Key": "gsk_...",
  "Tier": 1,
  "Models": [
    "llama-3.3-70b-versatile",
    "mixtral-8x7b-32768"
  ]
}
```

### 2. Cohere (Tier 1)
Enterprise-grade models.

```json
"Cohere": {
  "Type": "Cohere",
  "Key": "your_cohere_key",
  "Tier": 1,
  "Models": [ "command-r", "command-r-plus" ]
}
```

### 3. Cloudflare Workers AI (Tier 1)
Serverless inference.

```json
"Cloudflare": {
  "Type": "Cloudflare",
  "Key": "your_cf_token",
  "AccountId": "your_account_id",
  "Tier": 1,
  "Models": [ "@cf/meta/llama-3.1-8b-instruct" ]
}
```

### 4. OpenRouter (Tier 2 - Fallback)
A comprehensive aggregator often used as a fallback if direct providers fail.

```json
"OpenRouter": {
  "Type": "OpenRouter",
  "Key": "sk-or-...",
  "Tier": 2,
  "Models": [
    "llama-3.3-70b-versatile", 
    "mistralai/mistral-7b-instruct" 
  ]
}
```

*Note: In this example, if a request comes for `llama-3.3-70b-versatile`, Synaxis will first try **Groq** (Tier 1). If Groq fails, it will failover to **OpenRouter** (Tier 2).*

### 5. Pollinations (Free Tier)
No API key required.

```json
"Pollinations": {
  "Type": "Pollinations",
  "Tier": 1,
  "Models": [ "gpt-4o-mini", "mistral" ]
}
```
