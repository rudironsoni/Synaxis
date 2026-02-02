# Antigravity Verification Plan

To ensure the "Distinguished Engineer" quality of the implementation, we will add a comprehensive Unit Test suite before marking the task as complete.

## Location
`tests/InferenceGateway/Infrastructure.Tests/AntigravityChatClientTests.cs`

## Test Strategy
We will use `xUnit` and `Moq` to verify the `AntigravityChatClient` in isolation, mocking the `HttpClient` and `ITokenProvider`.

### Test Cases

#### 1. `GetResponseAsync_SendsCorrectRequest_ReturnsResponse`
*   **Goal**: Verify strict protocol compliance.
*   **Validation**:
    *   **Headers**: Assert `User-Agent` contains `antigravity/1.11.5`.
    *   **Auth**: Assert `Authorization` header uses the token from `ITokenProvider`.
    *   **Body**: Assert JSON structure matches `{ project: "...", model: "...", request: { ... } }`.
    *   **System Instructions**: Assert system messages are extracted to `request.systemInstruction`, NOT `request.contents`.

#### 2. `GetStreamingResponseAsync_ParsesSSE_YieldsUpdates`
*   **Goal**: Verify the custom SSE parser.
*   **Setup**: Mock a response stream containing multiple `data: { ... }` lines and a `[DONE]` signal.
*   **Validation**: Ensure all chunks are yielded and the stream closes gracefully.

#### 3. `Authentication_Retry_Logic`
*   **Goal**: Verify 401/403 handling (if implemented in client) or Token Provider usage.
*   **Validation**: Ensure `GetTokenAsync` is called before the request.

## Execution
1.  Create `AntigravityChatClientTests.cs`.
2.  Run `dotnet test`.
3.  Fix any regressions or spec mismatches found.
