# Synaxis Inference Gateway - Bruno API Collection

This directory contains the Bruno OpenCollection for testing the Synaxis Inference Gateway API.

## Collection Structure

```
Synaxis.InferenceGateway/
├── opencollection.yml          # Collection root configuration
├── environments/
│   ├── development.yml         # YAML environment (for Bruno GUI)
│   └── development.bru         # Bru environment (for Bruno CLI)
├── Authentication/
│   ├── folder.yml
│   ├── Register User.yml
│   ├── Register User.bru       # Native .bru format (CLI-ready)
│   ├── Login.yml
│   └── Dev Login.yml
├── Identity/
│   ├── folder.yml
│   ├── Identity Register.yml
│   ├── Identity Login.yml
│   ├── Refresh Token.yml
│   ├── Get Current User.yml
│   ├── Get Organizations.yml
│   └── Switch Organization.yml
└── API Keys/
    ├── folder.yml
    ├── Create API Key.yml
    ├── Revoke API Key.yml
    ├── Generate API Key (Legacy).yml
    ├── List API Keys.yml
    ├── Revoke API Key (Legacy).yml
    └── Get API Key Usage.yml
```

## File Formats

This collection provides requests in two formats:

1. **YAML (.yml)** - OpenCollection format, best used with Bruno GUI application
2. **Bru (.bru)** - Native Bruno format, optimal for Bruno CLI

Both formats are functionally equivalent and work with Bruno.

## Environment Variables

The collection uses the following environment variables:

- `baseUrl`: http://localhost:51121 (API base URL)
- `token`: JWT Bearer token for authenticated requests
- `adminToken`: Admin JWT token for administrative operations
- `apiKey`: API Key for OpenAI-compatible endpoints
- `projectId`: 550e8400-e29b-41d4-a716-446655440000
- `keyId`: 880e8400-e29b-41d4-a716-446655440003
- `organizationId`: 660e8400-e29b-41d4-a716-446655440001
- `refreshToken`: Refresh token for obtaining new access tokens
- `requestId`: aa0e8400-e29b-41d4-a716-446655440000

## Usage with Bruno GUI

1. Open Bruno application
2. Open Collection → Navigate to this directory
3. Select the `development` environment
4. Execute requests from the GUI

## Usage with Bruno CLI

### Prerequisites

```bash
npm install -g @usebruno/cli
```

### Run Individual Requests

```bash
# Using .bru format (recommended for CLI)
bru run "Authentication/Register User.bru" --env-file environments/development.bru

# Using .yml format
bru run "Authentication/Register User.yml" --env-file environments/development.bru
```

### Run Entire Folders

```bash
# Run all Authentication requests recursively
bru run Authentication -r --env-file environments/development.bru

# Run all Identity requests
bru run Identity -r --env-file environments/development.bru

# Run all API Keys requests
bru run "API Keys" -r --env-file environments/development.bru
```

### Run with Developer Sandbox

By default, Bruno CLI v3+ uses "safe" sandbox mode. To use "developer" mode:

```bash
bru run "Authentication/Register User.bru" --env-file environments/development.bru --sandbox developer
```

## API Endpoint Categories

### Authentication
- **Register User**: Create new user account
- **Login**: Authenticate and receive JWT token
- **Dev Login**: Development-only login endpoint

### Identity
- **Identity Register**: Register with organization
- **Identity Login**: Login with full token response
- **Refresh Token**: Get new access token using refresh token
- **Get Current User**: Retrieve authenticated user profile
- **Get Organizations**: List user's organizations
- **Switch Organization**: Change active organization context

### API Keys
- **Create API Key**: Generate new API key for a project
- **Revoke API Key**: Delete API key
- **Generate API Key (Legacy)**: Legacy endpoint with permissions and expiration
- **List API Keys**: View all API keys with optional revoked filter
- **Revoke API Key (Legacy)**: Legacy revocation with reason tracking
- **Get API Key Usage**: View API key usage statistics

## Converted from Postman

This collection was converted from the Postman collection at:
`Synaxis.InferenceGateway.PostmanCollection.json`

## Notes

- The collection structure follows Bruno OpenCollection YAML format specification
- Authentication tokens should be obtained from login endpoints and set in environment variables
- The server must be running at `http://localhost:51121` for requests to succeed
- Network errors (ENOTFOUND) indicate the server is not running or unreachable
