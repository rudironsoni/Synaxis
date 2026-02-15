// <copyright file="README.md" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

# Synaxis.Contracts V2

This directory is reserved for version 2 of the message contracts.

## Versioning Strategy

When introducing breaking changes to contracts:
1. Create new contract types in this V2 directory
2. Maintain V1 contracts for backward compatibility
3. Implement version negotiation in messaging layer
4. Document migration path from V1 to V2

## Example Structure

```
V2/Messages/
├── Event.cs (V2 base event with additional fields)
├── CreateTenantRequest.cs (V2 with new properties)
├── CreateTenantResponse.cs (V2 with new properties)
├── CreateUserRequest.cs (V2 with new properties)
└── CreateUserResponse.cs (V2 with new properties)
```
