# Plan: Integration of Kilo Code Free Inference

## Context
Kilo Code offers free inference for GLM 4.7 and MiniMax 2.1 via their API proxy.
- **Endpoint**: `https://api.kilo.ai/api/openrouter`
- **Models**: `glm-4.7`, `MiniMax-M2.1`
- **Headers**: 
  - `X-KiloCode-EditorName`: `Synaxis`
  - `X-KiloCode-Version`: `1.0.0`
  - `X-KiloCode-TaskId`: `synaxis-inference`

## Goal
Integrate Kilo Code as a supported provider in Synaxis Inference Gateway.

## Steps

### 1. Create KiloCodeChatClient
- **File**: `src/InferenceGateway/Infrastructure/External/KiloCode/KiloCodeChatClient.cs`
- **Description**: create a new client class that inherits from `GenericOpenAiChatClient`.
- **Implementation**:
  - Hardcode the Kilo Code API URL.
  - Inject the required `X-KiloCode-*` headers into the request.

### 2. Register Provider
- **File**: `src/InferenceGateway/Infrastructure/Extensions/InfrastructureExtensions.cs`
- **Action**: Add a new case `kilocode` in the `AddSynaxisInfrastructure` method.
- **Implementation**:
  - Register `KiloCodeChatClient` as a `KeyedSingleton<IChatClient>`.
  - Use the configuration key for the API token.

### 3. Update Configuration
- **File**: `src/InferenceGateway/WebApi/appsettings.Development.json`
- **Action**: Add the `KiloCode` provider configuration.
- **Content**:
  ```json
  "KiloCode": {
    "Type": "KiloCode",
    "Enabled": true,
    "Key": "<YOUR_KILO_TOKEN>",
    "Models": [ "glm-4.7", "MiniMax-M2.1" ]
  }
  ```

### 4. Unit Testing
- **File**: `tests/InferenceGateway/Infrastructure.Tests/External/KiloCode/KiloCodeChatClientTests.cs`
- **Goal**: Verify that the client is correctly instantiated and headers are present.
- **Coverage**: Ensure at least 80% code coverage.

### 5. Verification
- Build the solution to ensure no compilation errors.
- Run unit tests to verify behavior.
