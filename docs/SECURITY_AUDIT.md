# Security audit and hardening report

**Audit dates:** 2026-10-18 and 2026-10-19
**Scope:** ASP.NET Core API, React/Vite client, data model, dependency manifests, tests, and repository hygiene

## Executive result

The original implementation had several high-impact authorization and configuration weaknesses. The current tree fixes the identified application-level issues and adds regression tests so the same bypasses are less likely to return.

This is not a claim that the software is vulnerability-free. The review did not include a deployed infrastructure penetration test, cloud configuration review, browser automation attack suite, or third-party source-code audit.

## Fixed findings

| Severity | Finding | Resolution |
|---|---|---|
| Critical | A committed patch artifact exposed an old database password and JWT signing key | Artifact removed from the current tree; rotation and history rewrite are required |
| Critical | JWT validation accepted tokens without issuer, audience, or lifetime validation and had a fallback signing key | All validation enabled; startup now fails on missing/weak configuration; no fallback key |
| High | Any teacher could modify/delete any assignment and grade/read every submission | Every teacher operation now requires a matching class-subject allocation |
| High | A student could submit directly to another class's assignment | Submission creation now rechecks published state and student class membership |
| High | Fixed demo credentials were seeded automatically | Seeding is disabled by default and requires explicitly supplied passwords |
| High | Preview .NET packages introduced vulnerable transitive build/cryptography packages | Framework packages moved to patched stable .NET 10 releases |
| High | Frontend development dependency contained a denial-of-service advisory | Lockfile updated; full npm audit reports no known vulnerabilities |
| High | Two separate class entities/tables could split admin data from assignment foreign keys | Model unified; migration merges legacy data by class name and section before replacing foreign keys |
| Medium | CORS allowed every origin, method, and header | Exact configured origins plus a limited method/header set |
| Medium | Login had no brute-force control and leaked a timing difference for unknown accounts | Per-IP login limiting plus dummy BCrypt verification |
| Medium | Password/role changes did not invalidate existing tokens | Per-user token version is checked on every authenticated request |
| Medium | Negative marks, arbitrary statuses, unbounded pages, and oversized text were accepted | DTO and database constraints added; valid state transitions enforced |
| Medium | Students could overwrite graded or rejected work | Terminal states now return conflict; resubmission clears stale marks/feedback |
| Medium | Allocation data was readable by any authenticated role | Allocation endpoints are admin-only |
| Medium | Duplicate users, submissions, allocations, subjects, settings, and classes were race-prone | Unique database indexes added |
| Low | Swagger used an API-key description for bearer tokens | OpenAPI security scheme corrected to HTTP bearer JWT |
| Low | Frontend hard-coded an insecure API URL and persisted tokens across browser restarts | Relative `/api` proxy support and session storage added |

## Mandatory actions before deployment

1. **Rotate the exposed database password and JWT signing key.** Assume both were copied. Rotation is required even if they were only used locally.
2. **Remove the leaked artifact from Git history.** The current commit deletes it, but older commits remain downloadable.
3. **Revoke old JWTs.** Replacing the signing key invalidates every token issued with the exposed key.
4. **Back up the database, then run the new migration in staging first.** The migration merges the old duplicate class table into `Classes` and adds uniqueness/check constraints.
5. **Set production configuration through a secret manager.** Never put real values in `.env.example`, appsettings files, CI YAML, or GitHub issue text.

### Coordinated history cleanup

History rewriting changes commit IDs and requires every collaborator to re-clone or carefully rebase. Do this only after coordinating with all users of the repository:

```powershell
pipx install git-filter-repo
git filter-repo --path-glob "ecrets to local config and ignore logs*" --invert-paths
git remote add origin https://github.com/Joy-Kumar-Bhuiyan/Assignment-SubmissionManagementSystem.git
git push origin --force --all
git push origin --force --tags
```

`git filter-repo` normally removes the `origin` remote as a safety measure; add it back only after checking the rewritten history. Afterward, verify the GitHub file history no longer exposes the artifact. History cleanup does **not** replace secret rotation.

## Security design notes

- Authorization uses server-side role checks plus object-level class/subject ownership checks.
- Tokens expire after a configurable 5-120 minute window; the default is 60 minutes.
- The API checks the user's current email, role, and token version from the database for every JWT-authenticated request.
- BCrypt uses work factor 12. Password inputs are bounded to avoid excessive resource consumption.
- Late submissions default to denied if the setting is absent or malformed.
- Migrations and demo seeding are opt-in at startup so a production process cannot silently change schema or create known accounts.
- API responses include clickjacking, MIME-sniffing, referrer, and browser-permission hardening headers.

## Remaining risk and roadmap

## 2026-10-19 feature security review

### Fixed

- Audit access is Admin-only, paginated, and uses bounded filters. Controlled descriptions exclude passwords, hashes, tokens, answers, feedback, request bodies, connection strings, and provider credentials.
- Dashboard and XML export queries apply Student class/identity or Teacher allocation scope before aggregation/projection.
- Assignment, submission, and user sort fields use fixed allowlists; filters remain parameterized EF Core expressions and run before pagination.
- Webhook URLs come only from trusted deployment configuration. Production requires an absolute HTTPS URL and a 1–30 second timeout; there is no runtime arbitrary-URL endpoint.
- XML is output-only DTO serialization. The application does not accept or manually parse XML, so DTD/external-entity resolution is not enabled.
- SMTP and webhook failures are caught after the business database save, logged without credentials/payloads, and tested not to roll back a grade.

### Checked

- Existing JWT, role, teacher-allocation, student-class/ownership, deadline, state-transition, marks, pagination, DTO, CORS, rate-limit, exception, header, and uniqueness controls remain in place.
- New endpoints require authentication; audit events additionally require Admin. Student and Teacher reporting/export scope comes from validated identity rather than client-supplied email.
- Invalid export format, filter values, date ranges, page sizes, IDs, statuses, and oversized strings receive validation errors.
- No raw SQL or user-composed SQL was added. MSSQL examples are documentation only and use parameters in the stored-procedure example.

### Remaining risks

- `AuditEvents` is an ordinary mutable application table, not an append-only/tamper-evident compliance store.
- Creating an integer-key entity and its audit row requires two `SaveChangesAsync` calls so the generated entity ID is known. A rare audit-write failure after the core insert could leave that change without its audit row. A transactional interceptor/outbox is a future option if compliance requirements justify it.
- Email/webhook delivery is best effort. There is no retry queue, idempotency key, signature, or durable outbox.
- SMTP recipient addresses appear in operational logs when delivery is disabled/fails; production log access and retention must be restricted.
- EF InMemory tests do not prove PostgreSQL translation/constraint behavior for every new query.

### Deployment requirements for new features

- Apply the `AddPortfolioFeatures` migration in staging before production.
- Keep `Email:Enabled` and `Webhook:Enabled` false until secrets/endpoints are configured through a secret manager and tested.
- Restrict who can change deployment environment variables. A trusted operator could still configure a webhook to an internal destination.
- Use an approved SMTP account/from address, HTTPS webhook receiver, centralized logs with access controls, and monitoring for repeated provider failures.
- Retain only the audit events and operational logs needed by the application's real policy.

### Highest priority

- Move browser authentication to short-lived access tokens delivered through an HttpOnly, Secure, SameSite cookie strategy with CSRF protection.
- Add account lockout/backoff backed by Redis or an API gateway for multi-instance deployments.
- Add persistent security audit events for login, user management, grading, allocation, and settings changes.
- Add password reset, verified email flow, and optional MFA for administrators.

### Product and operational strength

- Add integration tests against a real ephemeral PostgreSQL instance; EF InMemory tests do not reproduce every relational behavior.
- Add end-to-end browser tests for all three roles.
- Add backups/restore drills, structured monitoring, alerting, and a documented incident-response process.
- Add file uploads only with type verification, size enforcement, random object names, malware scanning, and private object storage.
- Add deployment manifests with TLS, a non-root container user, read-only filesystem where practical, and least-privilege database credentials.

## Verification performed

- Backend compilation and xUnit regression suite.
- Frontend production build and lint.
- NuGet direct/transitive vulnerability scan.
- npm production and full development dependency audits.
- Manual review of authentication, authorization, input validation, CORS, secret handling, logging, data relationships, and workflow state changes.
