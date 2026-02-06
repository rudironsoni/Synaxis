# Synaxis SaaS API Test Collection - Test Execution Guide

## 📊 Collection Statistics

- **Total Tests**: 59 comprehensive API test files
- **Test Categories**: 10 major sections
- **Environments**: 3 (Development, Staging, Production)
- **Coverage**: Authentication, Multi-tenancy, Inference, Billing, Compliance, Admin

---

## 🧪 Test Coverage Summary

### 01-Authentication (7 tests)
✅ User registration with GDPR consent
✅ Email/password login with JWT tokens
✅ Email verification flow
✅ MFA setup (TOTP) with backup codes
✅ MFA login with device trust
✅ Token refresh with rotation
✅ Secure logout with token revocation

### 02-Organizations (6 tests)
✅ Organization creation with regional data residency
✅ Organization retrieval with usage stats
✅ Organization updates (metadata only)
✅ Organization deletion (GDPR compliant)
✅ Organization listing with pagination
✅ Quota limits and usage tracking

### 03-Teams (6 tests)
✅ Team creation within organizations
✅ Team retrieval with member list
✅ Team updates (settings, roles)
✅ Team deletion with cascade effects
✅ Team listing with filters
✅ User invitations with expiry

### 04-Users (6 tests)
✅ User profile retrieval
✅ User profile updates
✅ User deletion (GDPR Right to Erasure)
✅ User data export (GDPR Article 20)
✅ Cross-border consent management
✅ Team member listing

### 05-Virtual Keys (7 tests)
✅ API key creation with scopes and rate limits
✅ API key metadata retrieval (key never returned)
✅ API key updates (name, limits, models)
✅ API key revocation (immediate effect)
✅ API key listing with status filters
✅ API key usage analytics
✅ API key rotation with grace period

### 06-Inference (4 tests)
✅ Chat completion streaming (SSE)
✅ Chat completion non-streaming (JSON)
✅ Available models listing
✅ Model information and pricing

### 07-Quota & Billing (6 tests)
✅ Current usage with real-time stats
✅ Usage reports with date ranges
✅ Credit top-up with Stripe integration
✅ Credit balance retrieval
✅ Invoice listing
✅ Invoice PDF download

### 08-Compliance (4 tests)
✅ GDPR data export (Article 20)
✅ GDPR account deletion (Article 17)
✅ Privacy settings retrieval
✅ Consent management with audit trail

### 09-Admin (6 tests)
✅ Super admin organization listing
✅ Super admin organization details
✅ User impersonation for support
✅ Cross-border transfer monitoring
✅ Global analytics dashboard
✅ System health monitoring

### 10-Health (4 tests)
✅ Basic health check
✅ Readiness probe (Kubernetes)
✅ Liveness probe (Kubernetes)
✅ Multi-region health status

---

## 🚀 Quick Start Guide

### 1. Install Bruno CLI

```bash
# npm
npm install -g @usebruno/cli

# Homebrew (macOS)
brew install bruno

# Linux
snap install bruno
```

### 2. Test Collection Structure

```
collections/Synaxis.SaaS/
├── README.md                    # Main documentation
├── bruno.json                   # Collection config
├── environments/                # 3 environments
│   ├── development.bru
│   ├── staging.bru
│   └── production.bru
└── [01-10]-*/                   # 59 test files organized by category
```

### 3. Run Tests

```bash
# Run entire collection
bruno run collections/Synaxis.SaaS/ --env development

# Run specific category
bruno run collections/Synaxis.SaaS/01-Authentication/ --env development

# Run single test
bruno run collections/Synaxis.SaaS/01-Authentication/"Register User.bru" --env development

# Run with output
bruno run collections/Synaxis.SaaS/ --env staging --output results.json --format junit
```

---

## 🔄 Recommended Test Execution Order

### Sequential Flow (Happy Path)

```bash
# 1. Authentication
bruno run collections/Synaxis.SaaS/01-Authentication/

# 2. Organizations
bruno run collections/Synaxis.SaaS/02-Organizations/

# 3. Teams
bruno run collections/Synaxis.SaaS/03-Teams/

# 4. Virtual Keys
bruno run collections/Synaxis.SaaS/05-Virtual\ Keys/

# 5. Inference
bruno run collections/Synaxis.SaaS/06-Inference/

# 6. Usage & Billing
bruno run collections/Synaxis.SaaS/07-Quota\ \&\ Billing/

# 7. Health Checks
bruno run collections/Synaxis.SaaS/10-Health/
```

### Parallel Execution (Independent Tests)

```bash
# Health checks (no auth required)
bruno run collections/Synaxis.SaaS/10-Health/ &

# After authentication token obtained:
bruno run collections/Synaxis.SaaS/02-Organizations/ &
bruno run collections/Synaxis.SaaS/04-Users/ &
wait
```

---

## 📋 Test Assertions

Each test includes comprehensive assertions:

### Status Code Assertions
```javascript
res.status: eq 200
res.status: eq 201
res.status: eq 204
res.status: eq 401
res.status: eq 403
res.status: eq 404
res.status: eq 429
```

### Response Body Assertions
```javascript
res.body.id: isDefined
res.body.email: eq {{email}}
res.body.slug: matches ^[a-z0-9-]+$
res.body.status: eq active
```

### Header Assertions
```javascript
res.headers.content-type: contains application/json
res.headers.location: isDefined
res.headers.x-request-id: isDefined
```

### Custom JavaScript Tests
```javascript
test("User ID is UUID v4", function() {
  const uuidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
  expect(res.body.id).to.match(uuidRegex);
});
```

---

## 🔐 Authentication Flow

### Token Management

```javascript
// Pre-request: Check token expiry
const tokenExpiry = bru.getEnvVar("tokenExpiry");
if (Date.now() > parseInt(tokenExpiry)) {
  // Token expired, refresh it
}

// Post-response: Save tokens
if (res.body.accessToken) {
  bru.setEnvVar("authToken", res.body.accessToken);
  bru.setEnvVar("refreshToken", res.body.refreshToken);
}
```

### Automatic Token Refresh

The collection automatically handles:
- Token storage in environment variables
- Token expiry tracking
- Automatic token refresh when needed
- Token rotation on logout

---

## 🌍 Multi-Region Testing

Test cross-border data routing:

```bash
# EU region
bruno run collections/Synaxis.SaaS/ --env development --env-var region=eu-central-1

# US region
bruno run collections/Synaxis.SaaS/ --env development --env-var region=us-east-1

# APAC region
bruno run collections/Synaxis.SaaS/ --env development --env-var region=ap-southeast-1
```

---

## 🧹 Cleanup After Testing

```bash
# Delete test organizations
# Delete test API keys
# Revoke test tokens
# (Create cleanup scripts as needed)
```

---

## 📊 CI/CD Integration

### GitHub Actions Example

```yaml
name: API Tests

on: [push, pull_request]

jobs:
  api-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Install Bruno CLI
        run: npm install -g @usebruno/cli
      
      - name: Run API Tests
        run: |
          bruno run collections/Synaxis.SaaS/ \
            --env staging \
            --output test-results.json \
            --format junit
      
      - name: Upload Results
        uses: actions/upload-artifact@v3
        with:
          name: test-results
          path: test-results.json
```

### Jenkins Pipeline

```groovy
pipeline {
    agent any
    stages {
        stage('API Tests') {
            steps {
                sh 'npm install -g @usebruno/cli'
                sh 'bruno run collections/Synaxis.SaaS/ --env staging'
            }
        }
    }
}
```

---

## 🐛 Debugging Failed Tests

### Verbose Output
```bash
bruno run collections/Synaxis.SaaS/ --verbose
```

### Single Test Debugging
```bash
# Run single test with full output
bruno run collections/Synaxis.SaaS/01-Authentication/"Login.bru" --env development --verbose
```

### Check Environment Variables
```bash
# Print current environment
bruno env list collections/Synaxis.SaaS/ --env development
```

---

## 📈 Performance Testing

```bash
# Run inference tests multiple times
for i in {1..100}; do
  bruno run collections/Synaxis.SaaS/06-Inference/ --env staging
done
```

---

## 🔒 Security Testing Checklist

- ✅ Authentication required for protected endpoints
- ✅ JWT token validation and expiry
- ✅ API key authentication and scopes
- ✅ RBAC permissions (admin/member/readonly)
- ✅ GDPR compliance (data export, deletion)
- ✅ Cross-border consent enforcement
- ✅ Rate limiting (429 responses)
- ✅ Input validation (400/422 responses)
- ✅ Multi-tenancy isolation
- ✅ Audit logging for sensitive operations

---

## 📚 Additional Resources

- [Bruno Documentation](https://docs.usebruno.com)
- [Synaxis API Documentation](https://docs.synaxis.ai)
- [GDPR Compliance Guide](https://gdpr.eu)
- [OpenAPI Specification](../api/openapi.yaml)

---

## 🤝 Contributing

To add new tests:

1. Create `.bru` file in appropriate folder
2. Follow naming convention: `Action Resource.bru`
3. Include comprehensive assertions (status, body, headers)
4. Add pre/post scripts for state management
5. Document in README.md
6. Test in development environment first

---

## 📊 Test Metrics

Expected test execution times:

- **Full Collection**: ~5-10 minutes
- **Authentication**: ~30 seconds
- **Organizations**: ~20 seconds
- **Teams**: ~20 seconds
- **Users**: ~15 seconds
- **Virtual Keys**: ~30 seconds
- **Inference**: ~1-2 minutes (with API calls)
- **Billing**: ~20 seconds
- **Compliance**: ~30 seconds
- **Admin**: ~25 seconds
- **Health**: ~5 seconds

Total: **~5-10 minutes** for complete test suite

---

## ✅ Success Criteria

A successful test run should show:

- ✅ All 59 tests passing
- ✅ 0 failed assertions
- ✅ All tokens refreshed correctly
- ✅ All resources created/deleted properly
- ✅ All GDPR operations logged
- ✅ All rate limits respected
- ✅ All multi-region routing working

---

**Collection Version**: 1.0.0  
**Last Updated**: 2026-02-05  
**Maintained By**: Synaxis Platform Team
