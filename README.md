# Assignment & Submission Management System

[![CI](https://github.com/Joy-Kumar-Bhuiyan/Assignment-SubmissionManagementSystem/actions/workflows/ci.yml/badge.svg)](https://github.com/Joy-Kumar-Bhuiyan/Assignment-SubmissionManagementSystem/actions/workflows/ci.yml)

A role-based assignment platform built to practise maintainable backend design with ASP.NET Core. Administrators manage academic data, allocated teachers publish and grade work, and students can access and submit only work belonging to their class. The React interface consumes the same authenticated REST API documented in Swagger.

## Screenshots to add

Add these four real screenshots here after running the application:

1. `docs/images/login.png` — login screen.
2. `docs/images/student-assignments.png` — student assignment list.
3. `docs/images/teacher-grading.png` — teacher grading a submission.
4. `docs/images/swagger-authorize.png` — Swagger with the JWT Authorize dialog.

## Features by role

| Role | Capabilities |
|---|---|
| Admin | Manage users, classes, subjects, allocations and settings; view dashboards and audit events; access all assignment and submission records |
| Teacher | Manage assignments and submissions only for allocated class-subject pairs; grade, return or reject work; read status history |
| Student | View published assignments for their class; create or revise their own submissions; view only their own results |

```text
UnderReview -> Graded
UnderReview -> Returned -> UnderReview
UnderReview -> Rejected
```

Every status transition is stored in `SubmissionStatusHistories` with the previous status, new status, actor, role and UTC timestamp. Broader application actions are stored separately as `AuditEvents`.

## Tech stack

- ASP.NET Core 10 Web API, C#, Entity Framework Core 10, Npgsql and PostgreSQL
- JWT bearer authentication and BCrypt password hashing
- React 19 and Vite
- xUnit, FluentAssertions and EF Core InMemory
- Swagger/OpenAPI, Serilog and ASP.NET Core health checks
- GitHub Actions, Dependabot and CodeQL

## Architecture

```text
React/Vite UI
     |
     | HTTPS + JSON + JWT
     v
ASP.NET Core controllers (validation and HTTP mapping)
     |
     +--> access, audit and notification services
     |
     v
EF Core DbContext --> PostgreSQL
     |
     +--> optional SMTP provider
     +--> optional HMAC-signed webhook receiver
```

The project deliberately stays a single application. It does not add CQRS, MediatR, generic repositories, queues or microservices without a demonstrated need.

## Fresh-clone setup

### Prerequisites

- Git
- .NET SDK 10
- Node.js 24 or a compatible active LTS release
- PostgreSQL with an empty database named `SchoolDb`

```bash
git clone https://github.com/Joy-Kumar-Bhuiyan/Assignment-SubmissionManagementSystem.git
cd Assignment-SubmissionManagementSystem
dotnet tool restore
```

ASP.NET Core does **not** load `.env` automatically. `.env.example` is a deployment-variable reference only. For local development, use .NET User Secrets.

Windows PowerShell:

```powershell
cd BackendApi
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=SchoolDb;Username=postgres;Password=YOUR_POSTGRES_PASSWORD"
dotnet user-secrets set "Jwt:Key" "replace-with-at-least-32-random-bytes"
dotnet user-secrets set "SeedData:Enabled" "true"
dotnet user-secrets set "SeedData:AdminPassword" "AdminDemo!2026"
dotnet user-secrets set "SeedData:TeacherPassword" "TeacherDemo!2026"
dotnet user-secrets set "SeedData:StudentPassword" "StudentDemo!2026"
dotnet ef database update
dotnet run --launch-profile https
```

macOS/Linux:

```bash
cd BackendApi
dotnet user-secrets set 'ConnectionStrings:DefaultConnection' 'Host=localhost;Port=5432;Database=SchoolDb;Username=postgres;Password=YOUR_POSTGRES_PASSWORD'
dotnet user-secrets set 'Jwt:Key' 'replace-with-at-least-32-random-bytes'
dotnet user-secrets set 'SeedData:Enabled' 'true'
dotnet user-secrets set 'SeedData:AdminPassword' 'AdminDemo!2026'
dotnet user-secrets set 'SeedData:TeacherPassword' 'TeacherDemo!2026'
dotnet user-secrets set 'SeedData:StudentPassword' 'StudentDemo!2026'
dotnet ef database update
dotnet run --launch-profile https
```

Swagger: `https://localhost:5001/swagger`

Health: `https://localhost:5001/health`

In a second terminal, from the repository root:

```bash
cd FrontendWeb
npm ci
npm run dev
```

Open `http://localhost:5173`. Vite proxies `/api` to the local HTTPS API.

## Configuration

Use .NET User Secrets locally or environment variables in deployment. Nested keys use double underscores in environment variables, for example `Jwt__Key`.

| Setting | Required | Default | Purpose |
|---|---:|---|---|
| `ConnectionStrings:DefaultConnection` | Yes outside tests | none | PostgreSQL connection string |
| `Jwt:Key` | Yes | none | Signing key; startup fails below 32 UTF-8 bytes |
| `Jwt:Issuer` | Yes | `AssignmentManagementSystem` | Expected token issuer |
| `Jwt:Audience` | Yes | `AssignmentManagementSystemUsers` | Expected token audience |
| `Jwt:ExpiryMinutes` | No | `60` | Token lifetime, clamped to 5–120 minutes |
| `Cors:AllowedOrigins:0` | Yes | `http://localhost:5173` | Trusted frontend origin; add `:1`, `:2`, etc. for more |
| `Database:MigrateOnStartup` | No | `false` | Applies EF migrations at startup |
| `SeedData:Enabled` | No | `false` | Creates demo users when the Users table is empty |
| `SeedData:AdminPassword` | When seeding | none | Local admin demo password, minimum 12 characters |
| `SeedData:TeacherPassword` | When seeding | none | Local teacher demo password, minimum 12 characters |
| `SeedData:StudentPassword` | When seeding | none | Local student demo password, minimum 12 characters |
| `Email:Enabled` | No | `false` | Enables SMTP; otherwise a log-only provider is used |
| `Email:Host`, `Port`, `UseSsl`, `Username`, `Password`, `FromAddress`, `FromName` | When email enabled | see `appsettings.json` | SMTP details |
| `Webhook:Enabled` | No | `false` | Enables outgoing events |
| `Webhook:Url` | When enabled | none | Trusted HTTPS receiver URL |
| `Webhook:Secret` | When enabled | none | Minimum 32-byte HMAC-SHA256 secret |
| `Webhook:TimeoutSeconds` | No | `5` | Timeout from 1 to 30 seconds |
| `VITE_API_BASE_URL` | No | `/api` | Frontend API base for separate hosting |

Webhooks include `X-Webhook-Signature: sha256=<hex digest>`, retry once for HTTP 408, 429 or 5xx, and never roll back a successful business operation when delivery fails.

## Demo accounts (local development only)

| Role | Email | Local demo password |
|---|---|---|
| Admin | `admin@school.com` | `AdminDemo!2026` |
| Teacher | `teacher@school.com` | `TeacherDemo!2026` |
| Student | `student@school.com` | `StudentDemo!2026` |

These examples are for a disposable local database only. Never reuse them elsewhere. `DemoSeedTests` verifies that the seeder creates BCrypt hashes matching these configured values.

## API summary

```text
POST   /api/auth/login
GET    /health

GET    /api/assignments?page=1&pageSize=20&classId=1&subjectId=2&status=published
POST   /api/assignments
PUT    /api/assignments/{id}
DELETE /api/assignments/{id}

GET    /api/submissions?page=1&pageSize=20&assignmentId=10&status=UnderReview
GET    /api/submissions/my
POST   /api/submissions
PUT    /api/submissions/{id}/grade
PATCH  /api/submissions/{id}/status
GET    /api/submissions/{id}/history

GET    /api/dashboard/summary
GET    /api/audit-events?action=submission.graded&fromDate=2026-10-01
GET    /api/integrations/assignments/export?format=json
GET    /api/integrations/assignments/export?format=xml

GET/POST/PUT/DELETE /api/users
GET/POST/PUT/DELETE /api/classes
GET/POST/PUT/DELETE /api/subjects
GET/POST/DELETE     /api/allocations
GET/POST            /api/settings
```

List endpoints return `totalItems`, `page`, `pageSize`, `totalPages` and `data`. `pageSize` is capped at 100. Swagger exposes schemas and a JWT **Authorize** button.

## Tests

```bash
dotnet restore AssignmentSubmissionManagementSystem.sln
dotnet build AssignmentSubmissionManagementSystem.sln --no-restore --configuration Release
dotnet test AssignmentSubmissionManagementSystem.sln --no-build --no-restore --configuration Release

cd FrontendWeb
npm ci
npm run lint
npm run build
npm audit --audit-level=high
```

Tests cover authentication throttling, role access, ownership and allocations, UTC deadline blocking, excessive marks, hidden drafts, validation and pagination, health, status history, audit events, dashboards, exports, HMAC webhooks, retry/disabled behavior and integration-failure isolation.

Tests use EF Core InMemory. They are useful for business rules but cannot prove PostgreSQL migrations, constraints, SQL translation, transactions or query performance. Add PostgreSQL container integration tests before production.

## Design decisions

- **State workflow:** allowed transitions prevent arbitrary statuses from bypassing rules.
- **Server-side authorization:** roles are backed by allocation, class and ownership checks.
- **UTC deadlines:** deadlines and history timestamps are stored and compared in UTC.
- **DTO boundaries:** controllers validate request DTOs and project responses instead of returning tracked EF entities.
- **Options pattern:** SMTP and webhook settings are validated at startup and disabled by default.
- **Best-effort notifications:** provider failures are logged after the database write and do not fail grading.
- **In-memory tests:** fast rule coverage with the PostgreSQL limitations stated above.

## How this relates to CRM systems

The same patterns appear in CRM work: users have roles and record ownership, teachers have scoped allocations, submissions move through controlled statuses, status history acts like an activity timeline, dashboards summarize pipeline state, and signed webhooks/email notify external systems without coupling them to the core transaction.

## Security notes

- Passwords are BCrypt hashes with work factor 12 and are never returned.
- JWT validation checks signature, issuer, audience, expiry and per-user token version.
- Login gives the same response for an unknown email and a wrong password and is rate limited.
- CORS uses configured origins rather than `AllowAnyOrigin`.
- Current files contain no real database password or JWT signing key.
- Older commits contain reusable development credentials. Read [`docs/SECURITY_AUDIT.md`](docs/SECURITY_AUDIT.md), rotate reused values and coordinate a history rewrite before treating the repository as clean.

## Known limitations

- No refresh tokens, password recovery, verified email, MFA or file upload.
- Browser tokens use session storage rather than HttpOnly cookies with a CSRF design.
- Notifications are best effort; there is no durable queue or transactional outbox.
- Rate limiting is in-process and needs a shared strategy for multiple instances.
- CI has no PostgreSQL container test or browser-level end-to-end test.
- Docker Compose is not included because it was not verified in this environment.

## What I would do next

Add PostgreSQL Testcontainers coverage, browser end-to-end tests, password recovery and MFA, operational tracing, and a transactional outbox only when reliable external delivery becomes a requirement.

## Additional documentation

- [Security audit](docs/SECURITY_AUDIT.md)
- [Changes explained](docs/CHANGES_EXPLAINED.md)
- [Three-minute demo script](docs/DEMO_SCRIPT.md)
- [Interview guide](docs/INTERVIEW_GUIDE.md)
