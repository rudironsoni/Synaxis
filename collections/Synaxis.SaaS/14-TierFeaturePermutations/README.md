# Tier Feature Permutations Test Suite

Comprehensive Bruno API tests covering ALL tier and feature access permutations for Synaxis SaaS platform.

## Overview

This test suite validates that:
- **Free tier** users are properly restricted from premium features
- **Pro tier** users have access to mid-tier features but not enterprise-only features
- **Enterprise tier** users have full access to all features
- Proper error messages and upgrade prompts are shown when features are unavailable
- Feature limits are correctly enforced per tier

## Test Matrix

### Tiers
- **Free**: Community tier with basic features
- **Pro**: Professional tier with advanced features ($99/month)
- **Enterprise**: Custom enterprise features (Contact Sales)

### Features Tested

#### Core Premium Features
1. **Multi-Geo Deployment** - Multi-region data replication
2. **SSO (SAML/OIDC)** - Single Sign-On integration
3. **Audit Logs** - Compliance audit trails
4. **Custom Backup** - Custom backup policies and destinations
5. **Priority Support** - 24/7 priority support channels
6. **Dedicated Infrastructure** - Private compute instances
7. **Custom Models** - Fine-tuned model deployment
8. **Advanced Analytics** - Detailed usage analytics

#### Additional Features
9. **Team Collaboration** - Team size limits
10. **SCIM Provisioning** - Automated user provisioning
11. **IP Whitelisting** - Network access controls
12. **Rate Limits** - API rate limit configuration
13. **Compliance Reports** - SOC2, HIPAA, GDPR reports
14. **API Key Expiry** - Custom expiration policies
15. **Data Retention** - Custom retention periods
16. **Private Endpoints** - VPC private connectivity
17. **Webhook Subscriptions** - Event webhooks
18. **SLA Guarantees** - Service level agreements
19. **Feature Toggles** - Beta feature access
20. **Log Export** - Request log exports

## Test Organization

### File Naming Convention
```
[seq]-[Tier] - [Feature] - [Access].bru
```

Examples:
- `01-Free Tier - Multi Geo - Denied.bru`
- `09-Pro Tier - Multi Geo - Allowed.bru`
- `17-Enterprise Tier - Multi Geo - Allowed.bru`

### Test Count
**51 comprehensive tests** covering:
- 8 Free tier denied tests (premium features)
- 8 Pro tier tests (mixed allowed/denied)
- 8 Enterprise tier allowed tests (all features)
- 27 additional edge case and limit tests

## Feature Availability Matrix

| Feature | Free | Pro | Enterprise |
|---------|------|-----|------------|
| Multi-Geo Deployment | ❌ | ✅ (2 regions) | ✅ (unlimited) |
| SSO (SAML/OIDC) | ❌ | ❌ | ✅ |
| Audit Logs | ❌ | ✅ (30d) | ✅ (1y) |
| Custom Backup | ❌ | ❌ | ✅ |
| Priority Support | ❌ | ❌ | ✅ (4h SLA) |
| Dedicated Infrastructure | ❌ | ❌ | ✅ |
| Custom Models | ❌ | ❌ | ✅ |
| Advanced Analytics | ❌ | ✅ (30d) | ✅ (1y) |
| Team Collaboration | ✅ (5 members) | ✅ (50 members) | ✅ (unlimited) |
| SCIM Provisioning | ❌ | ❌ | ✅ |
| IP Whitelisting | ❌ | ❌ | ✅ |
| Custom Rate Limits | ❌ | ❌ | ✅ |
| Compliance Reports | ❌ | ❌ | ✅ |
| API Key Expiry | 30d fixed | 1-365d | Never (optional) |
| Data Retention | 7d | 30d | Custom (up to 7y) |
| Private Endpoints | ❌ | ❌ | ✅ |
| Webhooks | 1 | 5 | Unlimited |
| SLA | None | 99.9% | 99.99% |
| Beta Features | ❌ | ❌ | ✅ |
| Log Export | ❌ | ✅ (email) | ✅ (S3, email, API) |

## Expected Response Patterns

### Feature Allowed (200 OK)
```json
{
  "config": {
    "status": "active",
    "feature": "multi_geo",
    ...
  }
}
```

### Feature Denied (403 Forbidden)
```json
{
  "error": {
    "code": "FEATURE_NOT_AVAILABLE",
    "message": "Multi-geo deployment is not available on your current plan",
    "requiredTier": "pro",
    "currentTier": "free"
  },
  "upgrade": {
    "url": "/upgrade",
    "availableTiers": ["pro", "enterprise"],
    "contactSales": false
  }
}
```

### Tier Limit Exceeded (403 Forbidden)
```json
{
  "error": {
    "code": "TIER_LIMIT_EXCEEDED",
    "message": "Team size limit exceeded for free tier",
    "currentLimit": 5,
    "requestedLimit": 10,
    "currentUsage": 5
  },
  "upgrade": {
    "url": "/upgrade",
    "recommendedTier": "pro"
  }
}
```

## Environment Variables Required

Each test requires organization IDs for different tiers:

```javascript
// In your Bruno environment file
{
  "freeOrgId": "org_free_12345",
  "proOrgId": "org_pro_67890",
  "enterpriseOrgId": "org_ent_abcde",
  "authToken": "your_jwt_token",
  "baseUrl": "https://api.synaxis.ai",
  "region": "us-east-1"
}
```

## Test Execution

### Run All Tests
```bash
bruno run collections/Synaxis.SaaS/14-TierFeaturePermutations
```

### Run Tests by Tier
```bash
# Free tier tests
bruno run collections/Synaxis.SaaS/14-TierFeaturePermutations --filter "Free Tier"

# Pro tier tests
bruno run collections/Synaxis.SaaS/14-TierFeaturePermutations --filter "Pro Tier"

# Enterprise tier tests
bruno run collections/Synaxis.SaaS/14-TierFeaturePermutations --filter "Enterprise Tier"
```

### Run Tests by Feature
```bash
# Multi-geo tests
bruno run collections/Synaxis.SaaS/14-TierFeaturePermutations --filter "Multi Geo"

# SSO tests
bruno run collections/Synaxis.SaaS/14-TierFeaturePermutations --filter "SSO"
```

### Run Tests by Access Type
```bash
# All denied tests
bruno run collections/Synaxis.SaaS/14-TierFeaturePermutations --filter "Denied"

# All allowed tests
bruno run collections/Synaxis.SaaS/14-TierFeaturePermutations --filter "Allowed"
```

## Key Test Scenarios

### 1. Free Tier Restrictions
Tests that free tier users receive proper 403 errors with:
- Clear error messages
- Upgrade prompts to Pro tier
- Contact sales prompts for Enterprise features
- Current vs required tier information

### 2. Pro Tier Access
Tests that Pro tier users:
- Can access Pro features (multi-geo, audit logs, analytics)
- Cannot access Enterprise-only features (SSO, custom backups)
- Receive appropriate upgrade prompts for Enterprise features

### 3. Enterprise Tier Full Access
Tests that Enterprise tier users:
- Have full access to all features
- Can configure advanced settings
- Have unlimited quotas where applicable
- Receive premium SLA guarantees

### 4. Feature Limits
Tests tier-specific limits:
- Team size limits (Free: 5, Pro: 50, Enterprise: unlimited)
- Webhook limits (Free: 1, Pro: 5, Enterprise: unlimited)
- Rate limits (per tier)
- Data retention periods
- API key expiry rules

### 5. Upgrade Scenarios
Tests upgrade prompts and downgrade protection:
- Proper upgrade URLs and pricing
- "Contact Sales" vs self-service upgrades
- Downgrade impact analysis
- Feature loss warnings

## Assertions

Each test includes comprehensive assertions:

### Success Cases (200 OK)
- Response body structure
- Feature configuration applied
- Correct tier-specific limits
- Status indicators

### Failure Cases (403 Forbidden)
- Error code matches expected value
- Error message contains feature name
- Required tier specified
- Current tier identified
- Upgrade information present
- Appropriate CTA (upgrade URL or contact sales)

## CI/CD Integration

These tests should be run:
1. **Before tier changes** - Verify feature access rules
2. **After deployments** - Ensure tier enforcement works
3. **On schedule** - Daily validation of tier boundaries
4. **Before pricing changes** - Validate tier features align with pricing

## Documentation

Each test includes comprehensive documentation covering:
- Feature description
- Expected behavior
- Tier availability
- Upgrade paths
- Related features
- Limits and quotas

## Related Test Suites

- `11-RegionRoutingPermutations` - Geographic data routing tests
- `12-QuotaPermutations` - Usage quota enforcement tests
- `13-ErrorScenarios` - Error handling and validation tests

## Maintenance

When adding new features:
1. Add tests for all three tiers (Free denied, Pro allowed/denied, Enterprise allowed)
2. Update the feature matrix in this README
3. Add proper assertions for tier boundaries
4. Include upgrade prompts in denied responses
5. Document tier-specific limits and behaviors

## Support

For questions about tier features or test failures:
- Slack: #saas-testing
- Email: saas-team@synaxis.ai
- Docs: https://docs.synaxis.ai/testing/tier-permutations
