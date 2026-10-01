# swiftbets-identity

SwiftBets accounts and tokens: registration, email verification, password reset, sign-in, account status, and the RS256 tokens every other service trusts.

| Part | What it does |
|---|---|
| `SwiftBets.Identity.Api` | `/.well-known/*` (JWKS), `/auth/token`, `/auth/refresh`, `/auth/revoke`, `/auth/register`, verification and reset, `/profile`, staff `/admin/users` |
| `SwiftBets.Identity.Application` | Grants, refresh rotation with reuse detection, registration with the 18+ age gate, lockout, account status rules |
| `SwiftBets.Identity.Infrastructure` | Dapper over SQL Server (`SbIdentity`, schema `accounts`), RSA signing, password hashing, SMTP account emails |
| `SwiftBets.Identity.Migrator` | DbUp migrations with rollbacks; `Migrator:ImportFromPlacement` copies accounts and live refresh tokens across once |

Images: `ghcr.io/remonenaidoo/swiftbets-identity` and `swiftbets-identity-migrator`.

## Build and test

```bash
../swiftbets-platform/scripts/fetch-shared-packages.sh .
dotnet test
```

Integration tests start SQL Server in Docker (Testcontainers).

Moved out of `swiftbets-placement` with its history (platform `scripts/split-repo.sh`).
