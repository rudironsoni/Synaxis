# Web Search CLI

A command-line tool that searches the web using Bing Chat API and generates AI-powered answers using Claude.

## Features

- 🔍 Search the web using Bing Chat
- 🤖 Get AI-generated answers from Claude
- 📚 Source attribution and citations
- ⚡ Fast and easy to use

## Installation

```bash
npm install
```

## Setup

1. Copy the example environment file:
```bash
cp .env.example .env
```

2. Edit `.env` and add your credentials:
   - **BING_COOKIE**: Get from bing.com after signing in (look for `_U` cookie)
   - **ANTHROPIC_API_KEY**: Get from https://console.anthropic.com/

## Usage

### Basic Usage

```bash
node index.js "What is the capital of France?"
```

### With Options

```bash
node index.js "Latest AI news" --model claude-3-opus-20240229 --conversation-style creative
```

### Global Installation

To use the `web-search` command globally:

```bash
npm link
```

Then you can use:
```bash
web-search "Your question here"
```

## Options

| Option | Description | Default |
|--------|-------------|---------|
| `-m, --model <model>` | Claude model to use | `claude-3-5-sonnet-20241022` |
| `-c, --conversation-style <style>` | Bing conversation style (creative/balanced/precise) | `balanced` |
| `-V, --version` | Output the version number | - |
| `-h, --help` | Display help for command | - |

## Examples

```bash
# Simple question
node index.js "What is TypeScript?"

# Research topic
node index.js "Latest developments in quantum computing 2024"

# Use creative mode for brainstorming
node index.js "Ideas for a new mobile app" --conversation-style creative

# Use precise mode for factual queries
node index.js "What is the speed of light?" --conversation-style precise
```

## Requirements

- Node.js 16+
- Bing account (for cookie)
- Anthropic API key

## License

ISC
