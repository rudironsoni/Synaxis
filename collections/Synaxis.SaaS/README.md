# Synaxis SaaS API Test Collection

Comprehensive Bruno API test suite for Synaxis multi-tenant SaaS platform with global compliance, quota management, and cross-border data routing.

## 📋 Collection Overview

This collection contains **50+ comprehensive API tests** covering:

- ✅ Authentication (OAuth 2.0, MFA, JWT)
- ✅ Multi-tenancy (Organizations, Teams, Users)
- ✅ Virtual API Keys (Creation, Rotation, Revocation)
- ✅ LLM Inference (Streaming & Non-streaming)
- ✅ Quota & Billing Management
- ✅ GDPR Compliance (Data Export, Right to Deletion)
- ✅ Cross-Border Data Routing
- ✅ Admin Operations
- ✅ Health & Monitoring

## 🚀 Quick Start

### 1. Install Bruno

```bash
# macOS
brew install bruno

# Linux
snap install bruno

# Windows
choco install bruno
```

### 2. Open Collection

```bash
bruno collections/Synaxis.SaaS/
```

### 3. Select Environment

Choose one of the pre-configured environments:
- **Development**: Local testing (http://localhost:8000)
- **Staging**: Pre-production environment
- **Production**: Live production API

### 4. Run Tests

```bash
# Run entire collection
bruno run collections/Synaxis.SaaS/

# Run specific folder
bruno run collections/Synaxis.SaaS/01-Authentication/

# Run with environment
bruno run collections/Synaxis.SaaS/ --env development
```

## 📁 Collection Structure

```
Synaxis.SaaS/
├── 01-Authentication/       # 7 tests - OAuth, MFA, tokens
├── 02-Organizations/        # 6 tests - CRUD, limits
├── 03-Teams/               # 6 tests - Team management
├── 04-Users/               # 6 tests - User profiles, GDPR
├── 05-Virtual Keys/        # 7 tests - API key lifecycle
├── 06-Inference/           # 4 tests - LLM completions
├── 07-Quota & Billing/     # 6 tests - Usage, credits, invoices
├── 08-Compliance/          # 4 tests - GDPR, privacy
├── 09-Admin/               # 6 tests - Super admin operations
└── 10-Health/              # 4 tests - Health checks
```

## 🔐 Authentication Flow

The collection uses a **sequential authentication flow**:

1. **Register User** → Creates new user account
2. **Login** → Returns access token (saved to `authToken`)
3. **Verify Email** → Activates account
4. **Setup MFA** → Enables 2FA (optional)
5. **Login with MFA** → 2FA authentication
6. **Refresh Token** → Renews access token
7. **Logout** → Invalidates session

All subsequent requests use `{{authToken}}` from the environment.

## 🌍 Multi-Region Support

The platform supports cross-border data routing:

- **EU Region**: `eu-central-1` (Frankfurt)
- **US Region**: `us-east-1` (Virginia)
- **APAC Region**: `ap-southeast-1` (Singapore)

Data residency compliance is automatically enforced based on organization's `primaryRegion`.

## 🧪 Test Coverage

### HTTP Status Codes
- ✅ 200 OK
- ✅ 201 Created
- ✅ 204 No Content
- ✅ 400 Bad Request
- ✅ 401 Unauthorized
- ✅ 403 Forbidden
- ✅ 404 Not Found
- ✅ 409 Conflict
- ✅ 422 Unprocessable Entity
- ✅ 429 Too Many Requests
- ✅ 500 Internal Server Error

### Validation Tests
- ✅ Required fields validation
- ✅ Email format validation
- ✅ Password strength validation
- ✅ Slug format validation
- ✅ Enum value validation
- ✅ JSON schema validation

### Business Logic Tests
- ✅ Quota enforcement
- ✅ Rate limiting
- ✅ Multi-tenancy isolation
- ✅ RBAC permissions
- ✅ Credit balance checks
- ✅ Cross-border consent
- ✅ Data residency compliance

### Edge Cases
- ✅ Expired tokens
- ✅ Invalid API keys
- ✅ Duplicate resources
- ✅ Orphaned resources
- ✅ Concurrent modifications
- ✅ Resource limits exceeded

## 📊 Assertions

Each test includes comprehensive assertions:

```javascript
assert {
  res.status: eq 201
  res.body.id: isDefined
  res.body.name: eq "{{orgName}}"
  res.body.slug: matches ^[a-z0-9-]+$
  res.body.createdAt: isDefined
  res.headers.content-type: contains application/json
}
```

## 🔄 Pre/Post Scripts

### Pre-Request Scripts
```javascript
// Generate unique slugs
const timestamp = Date.now();
bru.setVar("orgSlug", `test-org-${timestamp}`);

// Calculate signatures
const signature = crypto.createHmac('sha256', secret)
  .update(payload)
  .digest('hex');
bru.setVar("signature", signature);
```

### Post-Response Scripts
```javascript
// Save authentication tokens
if (res.body.accessToken) {
  bru.setEnvVar("authToken", res.body.accessToken);
  bru.setEnvVar("refreshToken", res.body.refreshToken);
}

// Save resource IDs for subsequent tests
if (res.body.id) {
  bru.setEnvVar("orgId", res.body.id);
}
```

## 🎯 Test Scenarios

### 1. Happy Path
Complete end-to-end user journey from registration to inference:
1. Register → Login → Create Org → Create Team → Create API Key → Chat Completion

### 2. Error Handling
Test all error scenarios:
- Invalid credentials
- Insufficient permissions
- Quota exceeded
- Invalid input data
- Resource not found

### 3. GDPR Compliance
Test data privacy features:
- Data export (JSON format)
- Right to deletion (cascading)
- Consent management
- Cross-border transfer controls

### 4. Quota Management
Test usage limits:
- Token consumption tracking
- Credit deduction
- Rate limit enforcement
- Auto top-up triggers

### 5. Multi-Tenancy
Test tenant isolation:
- Organization scoping
- Team permissions
- Resource access control
- Cross-org validation

## 🔧 Environment Variables

### Required Variables
```
baseUrl           # API base URL
authToken         # JWT access token
refreshToken      # JWT refresh token
orgId             # Current organization ID
teamId            # Current team ID
apiKey            # Virtual API key
region            # AWS region (eu-central-1, us-east-1, etc.)
```

### Generated Variables
```
orgSlug           # Unique organization slug
teamSlug          # Unique team slug
timestamp         # Current timestamp
userId            # User ID
invoiceId         # Invoice ID
```

## 📝 Best Practices

### 1. Sequential Execution
Run authentication tests first to populate tokens:
```bash
bruno run collections/Synaxis.SaaS/01-Authentication/ && \
bruno run collections/Synaxis.SaaS/02-Organizations/
```

### 2. Environment Isolation
Use separate environments for isolated testing:
```bash
# Development (safe)
bruno run --env development

# Production (caution!)
bruno run --env production --filter "Health Check"
```

### 3. Cleanup
Delete test resources after execution:
```bash
# Run cleanup script (if available)
bruno run collections/Synaxis.SaaS/99-Cleanup/
```

### 4. CI/CD Integration
```yaml
# .github/workflows/api-tests.yml
- name: Run API Tests
  run: |
    npm install -g @usebruno/cli
    bruno run collections/Synaxis.SaaS/ \
      --env staging \
      --output results.json \
      --format junit
```

## 🐛 Troubleshooting

### Token Expired
```bash
# Re-run authentication flow
bruno run collections/Synaxis.SaaS/01-Authentication/Login.bru
```

### Invalid API Key
```bash
# Create new API key
bruno run collections/Synaxis.SaaS/05-Virtual\ Keys/Create\ API\ Key.bru
```

### Quota Exceeded
```bash
# Top up credits
bruno run collections/Synaxis.SaaS/07-Quota\ \&\ Billing/Top\ Up\ Credits.bru
```

### Rate Limited
```bash
# Wait and retry
sleep 60 && bruno run <test>
```

## 📚 Additional Resources

- [Synaxis API Documentation](https://docs.synaxis.ai)
- [Bruno Documentation](https://docs.usebruno.com)
- [OpenAPI Specification](../api/openapi.yaml)
- [Postman Migration Guide](./POSTMAN_MIGRATION.md)

## 🤝 Contributing

To add new tests:

1. Create `.bru` file in appropriate folder
2. Follow naming convention: `Action Resource.bru`
3. Include comprehensive assertions
4. Add pre/post scripts as needed
5. Document in this README
6. Test in development environment

## 📄 License

MIT License - See LICENSE file for details

## 🔗 Links

- GitHub: https://github.com/yourusername/synaxis
- Issues: https://github.com/yourusername/synaxis/issues
- Docs: https://docs.synaxis.ai
