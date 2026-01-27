# Model Verification Tests

This directory contains scripts to verify the connectivity and availability of AI models configured in the Inference Gateway.

## `test_all_models.sh`

Iterates through all `CanonicalModels` defined in the gateway configuration and attempts a basic "Chat Completion" request.

### Usage
```bash
./test_all_models.sh
```

### Prerequisites
The Inference Gateway must be running (e.g., via Docker Compose or `dotnet run`).
Default target: `http://localhost:5042/v1/chat/completions`.

### Configuration
Update the `MODELS` array in the script if you add or remove models from `appsettings.json`.

### Interpreting Results
- **PASS**: The provider returned a valid JSON response with `choices`.
- **FAIL**: The provider returned an error (401 Unauthorized, 404 Not Found, 429 Too Many Requests, or 500 Internal Error).

*Note: Some failures may be due to expired trial keys or rate limits.*
