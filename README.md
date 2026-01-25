# Synaxis – Because Paying for AI Is for People With Self-Respect

<p align="center">
  <img src="https://img.shields.io/badge/.NET%2010-the%20future%20is%20free(ish)-blue?style=for-the-badge" alt=".NET 10">
  <img src="https://img.shields.io/badge/mindset-ULTRA%20MISER%20MODE™-orange?style=for-the-badge" alt="Ultra Miser Mode">
  <img src="https://img.shields.io/badge/spending%20money-never-brightgreen?style=for-the-badge" alt="Zero Spend Energy">
</p>

**Synaxis** — the dignified art project of routing prompts through every free inference crumb on the internet before anyone dares ask you for a credit card.

This isn't just software.
It's **ULTRA MISER MODE™**: a lifestyle choice. A philosophy. A quiet rebellion against subscription fatigue.
A lovingly architected reminder that tokens should be free, and if they're not, we'll just rotate until they are.

## The Core Gag (aka Why This Exists)

> Craving Claude-3.5 / GPT-4o / Llama-405B quality but your wallet is practicing minimalism?
> Welcome home, fellow connoisseur of other people's free tiers.

ULTRA MISER MODE™ exists for:

- People who flinch at $5 the way normal humans flinch at spiders
- Devs whose monthly burn rate is measured in leftover API credits
- Anyone who has ever whispered “just one more prompt” while watching Groq’s counter tick toward zero
- broke geniuses, caffeine addicts, and professional quota evaders worldwide

## Features (87% of which exist purely to provide free usage)

- One beautiful OpenAI-compatible `/v1` endpoint (so you don’t have to touch your client code ever again)
- Automatic, merciless rotation when any provider inevitably says “you’ve had enough generosity for today”
- Failover choreography so smooth it almost feels ethical
- Priority-based routing (burn the highest-quota ones first, obviously)
- Natively supports Groq, Cloudflare Workers AI, Together AI, DeepInfra, Fireworks, Cohere, Lepton, and whatever sad little free tier appears next Tuesday
- Zero euros spent until the heat death of the universe (or until someone actually sponsors this chaos)


## Key Features

*   **Unified API:** Access multiple LLM providers through a single, OpenAI-compatible interface.
*   **Intelligent Routing ("The Brain"):** Requests are routed based on the requested model ID.
*   **Tiered Failover:** Configure providers in tiers. If a Tier 1 provider fails, Synaxis automatically fails over to Tier 2, and so on.
*   **Load Balancing:** Requests within the same tier are shuffled to distribute load across available providers.
*   **Clean Architecture:** Structured for maintainability and testability (`Api`, `Application`, `Infrastructure`).
*   **Extensible:** Easily add new providers via the `IProviderRegistry` and `IChatClient` interface.

## Quick Start

### Prerequisites

*   [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
*   [Docker]()


## **1. Installation – Costs: €0.00 (electricity not included)**

```bash
# Clone before the guilt sets in
git clone https://github.com/rudironsoni/Synaxis.git
cd Synaxis

# Restore packages on someone else’s electricity bill
dotnet restore

# Build — because even misers deserve snappy startup
dotnet build
```

## **2. Configuring Your Miser Empire (The Only Section You’ll Actually Read):**

Open `src/Synaxis.WebApi/appsettings.json`.
Add your API keys for the providers you wish to use.
See [Configuration Guide](docs/CONFIGURATION.md) for details.

Quick info:

```json
{
  "Providers": [
    {
      "Name": "Groq",
      "BaseUrl": "https://api.groq.com/openai/v1",
      "ApiKey": "gsk_...(the precious few remaining requests)",
      "Models": ["llama-3.1-70b-versatile", "mixtral-8x7b-32768", "whatever-is-fast-and-free-today"],
      "MaxRequestsPerMinute": 999999,  // optimism
      "Priority": 1                    // dies first, heroically
    },
    {
      "Name": "Cloudflare",
      "BaseUrl": "https://api.cloudflare.com/client/v4/accounts/.../ai/v1",
      "ApiKey": "...",
      "Models": ["@cf/meta/llama-3-8b-instruct"],
      "Priority": 2                    // backup dancer
    },
    {
      "Name": "The-Next-Free-Tier-Victim",
      "Priority": 10                   // lives longest, suffers most
    }
  ],
  "UltraMiserMode": {
    "Enabled": true,
    "RotateOnRateLimit": true,
    "RotateOn429": true,
    "RotateOn403": true,               // "please stop loving us so much"
    "RotateOn existential-dread": true
  }
}
```

## **3. Running the Beast (While Mentally Thanking Providers for Their Charity):**
```bash
# Normal person way
dotnet run --project Synaxis.Api

# Miser-optimized (skip build if you're feeling extra cheap)
dotnet run --project Synaxis.Api --configuration Release
```

Then slam this into any OpenAI client:

```
http://localhost:5000/v1/chat/completions
```

And bask in the warm glow of 16k tokens costing literally nothing.

## Usage

Send an OpenAI-compatible request to the gateway. For example, using `curl`:

```bash
curl http://localhost:5000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "llama-3.3-70b-versatile",
    "messages": [
      { "role": "user", "content": "Hello, world!" }
    ]
  }'
```

Synaxis will inspect the `model` parameter, find the configured provider (e.g., Groq), and route the request accordingly.

## License

MIT — because monetizing a free-tier proxy would be performance art, not software.
ULTRA MISER MODE™ — not a feature. A way of life. Built with spite, clean architecture pride, and the tears of expired API keys.

Enjoy (or hoard — we don’t judge).

There, still savage, but now it lovingly embraces the beautiful, over-engineered madness that is Synaxis.
Proud .NET 10 energy all the way.

## Documentation

*   [Architecture Overview](docs/ARCHITECTURE.md)
*   [Configuration Guide](docs/CONFIGURATION.md)
