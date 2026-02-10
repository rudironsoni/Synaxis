# Failing Integration Tests Report

**Total Tests:** 628  
**Passed:** 584  
**Failed:** 21  
**Skipped:** 23  

## List of 21 Failing Tests

### TeamMembershipsControllerTests (14 failures)

| Test Name | Expected Status | Actual Status | Error Message |
|-----------|----------------|---------------|---------------|
| RemoveMember_RemoveSelf_ReturnsNoContent | 204 NoContent | 500 InternalServerError | Expected response.StatusCode to be HttpStatusCode.NoContent {value: 204}, but found HttpStatusCode.InternalServerError {value: 500}. |
| RemoveMember_ValidRequest_ReturnsNoContent | 204 NoContent | 500 InternalServerError | Expected response.StatusCode to be HttpStatusCode.NoContent {value: 204}, but found HttpStatusCode.InternalServerError {value: 500}. |
| RemoveMember_MemberNotFound_ReturnsNotFound | 404 NotFound | 500 InternalServerError | Expected response.StatusCode to be HttpStatusCode.NotFound {value: 404}, but found HttpStatusCode.InternalServerError {value: 500}. |
| RemoveMember_NotTeamAdmin_ReturnsForbidden | 403 Forbidden | 500 InternalServerError | Expected response.StatusCode to be HttpStatusCode.Forbidden {value: 403}, but found HttpStatusCode.InternalServerError {value: 500}. |
| RemoveMember_WithoutAuth_ReturnsUnauthorized | 401 Unauthorized | 500 InternalServerError | Expected response.StatusCode to be HttpStatusCode.Unauthorized {value: 401}, but found HttpStatusCode.InternalServerError {value: 500}. |
| UpdateMemberRole_NotTeamAdmin_ReturnsForbidden | 403 Forbidden | 500 InternalServerError | Expected response.StatusCode to be HttpStatusCode.Forbidden {value: 403}, but found HttpStatusCode.InternalServerError {value: 500}. |
| UpdateMemberRole_WithoutAuth_ReturnsUnauthorized | 401 Unauthorized | 500 InternalServerError | Expected response.StatusCode to be HttpStatusCode.Unauthorized {value: 401}, but found HttpStatusCode.InternalServerError {value: 500}. |
| UpdateMemberRole_ValidRequest_ReturnsOk | 200 OK | 500 InternalServerError | Expected response.StatusCode to be HttpStatusCode.OK {value: 200}, but found HttpStatusCode.InternalServerError {value: 500}. |
| UpdateMemberRole_MemberNotFound_ReturnsNotFound | 404 NotFound | 500 InternalServerError | Expected response.StatusCode to be HttpStatusCode.NotFound {value: 404}, but found HttpStatusCode.InternalServerError {value: 500}. |
| UpdateMemberRole_InvalidRole_ReturnsBadRequest | 400 BadRequest | 500 InternalServerError | Expected response.StatusCode to be HttpStatusCode.BadRequest {value: 400}, but found HttpStatusCode.InternalServerError {value: 500}. |
| ListMembers_NotTeamMember_ReturnsForbidden | 403 Forbidden | 500 InternalServerError | Expected response.StatusCode to be HttpStatusCode.Forbidden {value: 403}, but found HttpStatusCode.InternalServerError {value: 500}. |
| ListMembers_ValidRequest_ReturnsMembersList | 200 OK | 500 InternalServerError | Expected response.StatusCode to be HttpStatusCode.OK {value: 200}, but found HttpStatusCode.InternalServerError {value: 500}. |
| ListMembers_SupportsPagination | 200 OK | 500 InternalServerError | Expected response.StatusCode to be HttpStatusCode.OK {value: 200}, but found HttpStatusCode.InternalServerError {value: 500}. |
| ListMembers_WithoutAuth_ReturnsUnauthorized | 401 Unauthorized | 500 InternalServerError | Expected response.StatusCode to be HttpStatusCode.Unauthorized {value: 401}, but found HttpStatusCode.InternalServerError {value: 500}. |

### TeamsControllerTests (6 failures)

| Test Name | Expected Status | Actual Status | Error Message |
|-----------|----------------|---------------|---------------|
| UpdateMemberRole_MemberNotFound_ReturnsNotFound | 404 NotFound | 500 InternalServerError | Expected response.StatusCode to be HttpStatusCode.NotFound {value: 404}, but found HttpStatusCode.InternalServerError {value: 500}. |
| UpdateMemberRole_InvalidRole_ReturnsBadRequest | 400 BadRequest | 500 InternalServerError | Expected response.StatusCode to be HttpStatusCode.BadRequest {value: 400}, but found HttpStatusCode.InternalServerError {value: 500}. |
| UpdateMemberRole_TeamNotFound_ReturnsNotFound | 404 NotFound | 500 InternalServerError | Expected response.StatusCode to be HttpStatusCode.NotFound {value: 404}, but found HttpStatusCode.InternalServerError {value: 500}. |
| UpdateMemberRole_NotTeamAdmin_ReturnsForbidden | 403 Forbidden | 500 InternalServerError | Expected response.StatusCode to be HttpStatusCode.Forbidden {value: 403}, but found HttpStatusCode.InternalServerError {value: 500}. |
| UpdateMemberRole_ValidRequest_ReturnsOk | 200 OK | 500 InternalServerError | Expected response.StatusCode to be HttpStatusCode.OK {value: 200}, but found HttpStatusCode.InternalServerError {value: 500}. |
| UpdateMemberRole_WithoutAuth_ReturnsUnauthorized | 401 Unauthorized | 500 InternalServerError | Expected response.StatusCode to be HttpStatusCode.Unauthorized {value: 401}, but found HttpStatusCode.InternalServerError {value: 500}. |

### AuthenticationControllerTests (1 failure)

| Test Name | Expected Status | Actual Status | Error Message |
|-----------|----------------|---------------|---------------|
| Refresh_ValidToken_ReturnsNewToken | 200 OK | 400 BadRequest | Expected response.StatusCode to be HttpStatusCode.OK {value: 200}, but found HttpStatusCode.BadRequest {value: 400}. |

## Root Cause Analysis

### Common Pattern
- **20 of 21 failures** return `500 InternalServerError` instead of expected status codes (2xx, 4xx).
- **1 failure** returns `400 BadRequest` instead of `200 OK`.

### Likely Causes
1. **Unhandled exceptions** in controller actions (null references, database errors, missing services).
2. **Authorization middleware** not properly configured for test environment.
3. **Database seeding** incomplete – missing required entities (organizations, teams, memberships).
4. **Service dependencies** not registered in `SynaxisWebApplicationFactory`.
5. **JWT token validation** mismatch – different signing key between token generation and validation.

## Suggested Fixes

### Immediate Actions
1. **Enable detailed error logging** in test environment to capture exception details.
2. **Review `SynaxisWebApplicationFactory`** – ensure all required services are registered (especially `ITeamMembershipService`, `ITeamService`, `IAuthenticationService`).
3. **Check database seeding** in test helpers (`CreateTestOrganizationAsync`, `CreateTestGroupAsync`, `AddUserToGroupAsync`).
4. **Add try-catch blocks** in controller actions to return appropriate error responses instead of throwing.

### Specific Investigations
- **TeamMembershipsController**: Verify `RemoveMember` and `UpdateMemberRole` methods handle missing entities gracefully.
- **TeamsController**: Same as above.
- **AuthenticationController**: Verify JWT secret consistency and token refresh logic.

### Test Environment
- Ensure `HttpClient` instances are authenticated with valid tokens.
- Confirm that test data cleanup does not interfere with other tests (parallel execution issues).

### Next Steps
1. Run a single failing test with debug output to see exact exception.
2. Check application logs during test execution (already available in TRX `StdOut`).
3. Fix one controller at a time, starting with TeamMembershipsController.

## Verification
After fixes, run:
```bash
dotnet test tests/InferenceGateway/IntegrationTests/ -c Release
```
Expect all 21 failures to pass.