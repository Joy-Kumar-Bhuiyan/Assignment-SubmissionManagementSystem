# Assignment & Submission Management System

A role-based school assignment workflow built as a junior-friendly full-stack portfolio project. Admins manage the academic structure, allocated teachers publish and review work, and students see and submit only assignments available to their class. The API adds practical reporting, traceability, and small external integrations without unnecessary architecture.

## Tech stack

- ASP.NET Core 10 Web API, C#, Entity Framework Core, Npgsql, PostgreSQL
- React 19 and Vite
- JWT bearer authentication, BCrypt, Swagger/OpenAPI
- xUnit, FluentAssertions, EF Core InMemory test host
- GitHub Actions, CodeQL, Dependabot, NuGet audit, npm audit

## Problem it solves

The system keeps users, classes, subjects, teacher allocations, assignments, and submissions in one workflow. Server-side authorization prevents a teacher from managing an unallocated class-subject pair and prevents a student from reading or changing another student's work.

## Roles

| Role | What the API allows |
|---|---|
| Admin | Manage users and academic structure, view all reporting, and read audit events |
| Teacher | Manage assignments and submissions only for allocated class-subject pairs |
| Student | View published assignments for their class, submit their own work, and view their own results |

Submission states follow:

```text
UnderReview -> Graded
UnderReview -> Returned -> UnderReview
UnderReview -> Rejected
```

## Key backend features

- REST/JSON API with DTO validation and bounded pagination
- JWT signature, issuer, audience, lifetime, current role/email, and per-user token-version validation
- Object-level authorization for assignment allocation, student class membership, and submission ownership
- Database-side search, filters, validated sorting, counts, grouping, and marks-percentage averages
- Persistent, Admin-only audit events with action/entity/actor/date filters
- Role-aware dashboard summaries for Admin, Teacher, and Student
- Optional SMTP email for publication, grading, and return-for-revision notifications
- Configuration-only outgoing JSON webhooks for publication, submission, and grading events
- Explicit role-scoped assignment export as JSON or XML
- PostgreSQL migrations, uniqueness/check constraints, and query-focused indexes
- Centralized Problem Details, login throttling, allowlisted CORS, HTTPS/HSTS, and response security headers

## Architecture

```text
React/Vite
    |
REST + JSON
    v
ASP.NET Core Controllers
    |
Small services (access, audit, notifications, email, webhook)
    |
Entity Framework Core + LINQ
    |
PostgreSQL

Optional outputs: SMTP provider, configured webhook receiver, XML export
```

This remains one understandable application. It does not use CQRS, MediatR, repositories over EF Core, message brokers, microservices, or event sourcing.

## Security decisions

- Authorization is enforced in controllers/services; hiding frontend buttons is never treated as security.
- Passwords are BCrypt hashes with work factor 12. Unknown-user login attempts still perform dummy BCrypt work.
- Password or role changes increment `TokenVersion`, invalidating existing JWTs.
- Email/webhook credentials come from configuration. They are not logged or stored in audit events.
- The webhook URL is trusted deployment configuration, not user input; production validation requires HTTPS and a 1–30 second timeout.
- XML is generated from DTOs with `XmlSerializer`; this endpoint does not parse incoming XML and therefore does not enable DTD/external-entity processing.
- Email and webhook calls happen after the database write; provider failure is logged and does not undo a successful grade, submission, or publication.

Read [the security audit](docs/SECURITY_AUDIT.md) before deploying. It includes mandatory rotation/history-cleanup work for secrets exposed in an older commit.

## API examples

```text
POST   /api/auth/login

GET    /api/assignments?search=sql&status=published&sortBy=dueDate&sortDirection=asc
POST   /api/assignments
PUT    /api/assignments/{id}
DELETE /api/assignments/{id}

GET    /api/submissions?status=UnderReview&assignmentId=10
GET    /api/submissions/my
POST   /api/submissions
PUT    /api/submissions/{id}/grade
PUT    /api/submissions/{id}/status

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

Swagger documents request/response schemas and accepts `Authorization: Bearer <token>`.

## Database

PostgreSQL is the application database. EF Core migrations create Users, Classes, Subjects, TeacherAllocations, Assignments, Submissions, SystemSettings, and AuditEvents. Unique indexes prevent duplicate users, classes, subjects, allocations, settings, and per-student assignment submissions. Check constraints protect assignment and mark ranges.

For SQL Server interview practice based on this real schema, see [MSSQL_PRACTICE.sql](docs/MSSQL_PRACTICE.sql) and [SQL_INTERVIEW_GUIDE.md](docs/SQL_INTERVIEW_GUIDE.md). They do not claim that SQL Server is a second runtime database.

## Run locally

### Prerequisites

- .NET SDK 10
- Node.js 24+
- PostgreSQL

### Backend configuration

ASP.NET Core does not automatically import `.env`. Use .NET User Secrets locally:

```powershell
cd BackendApi
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=SchoolDb;Username=postgres;Password=YOUR_DATABASE_PASSWORD"
dotnet user-secrets set "Jwt:Key" "YOUR_RANDOM_SIGNING_KEY_WITH_AT_LEAST_32_BYTES"
dotnet ef database update
dotnet run --launch-profile https
```

Swagger: `https://localhost:5001/swagger`

Health: `https://localhost:5001/health`

Demo seeding is off by default. To use it, set `SeedData:Enabled=true` and provide unique 12+ character Admin, Teacher, and Student passwords. The sample emails are `admin@school.com`, `teacher@school.com`, and `student@school.com`.

### Frontend

```powershell
cd FrontendWeb
npm ci
npm run dev
```

Open `http://localhost:5173`. Vite proxies `/api` to the HTTPS backend.

### Optional email and webhook

The application runs with both disabled. Configuration names and placeholders are in `.env.example`. Enable SMTP only with provider credentials stored in user secrets/a deployment secret manager. Enable the webhook only with a trusted HTTPS receiver URL. Delivery is best effort; there is no queue or retry scheduler.

## Tests and quality checks

```powershell
dotnet restore
dotnet build BackendApi.Tests/BackendApi.Tests.csproj -c Release --no-restore
dotnet test BackendApi.Tests/BackendApi.Tests.csproj -c Release --no-build --no-restore

cd FrontendWeb
npm ci
npm run lint
npm run build
npm audit --audit-level=high
```

The suite covers role and allocation boundaries, cross-class submission blocking, deadlines, state transitions, marks validation, pagination, audit authorization/content, dashboard scoping, filters, XML serialization, webhook JSON, notification triggers, integration-failure isolation, and the complete submission workflow.

## Documentation

- [Changes explained](docs/CHANGES_EXPLAINED.md)
- [Interview guide](docs/INTERVIEW_GUIDE.md)
- [Three-minute demo script](docs/DEMO_SCRIPT.md)
- [Resume notes](docs/RESUME_NOTES.md)
- [Conservative portfolio review](docs/PORTFOLIO_REVIEW.md)
- [SOAP integration notes](docs/SOAP_INTEGRATION_NOTES.md)
- [Security audit](docs/SECURITY_AUDIT.md)

## Known limitations

- Access tokens use browser session storage; an HttpOnly Secure SameSite cookie plus CSRF design would reduce XSS token exposure.
- There is no refresh-token flow, password recovery, verified-email flow, MFA, file upload, or production deployment manifest.
- Tests use EF InMemory and do not reproduce every PostgreSQL behavior; add containerized PostgreSQL integration tests before production.
- Audit events are useful application history, not an immutable compliance ledger.
- SMTP requires provider configuration. Webhook delivery is best effort and has no durable retry/outbox.
- Rate limiting is in-process; multi-instance deployments need a gateway/shared strategy.
- Historical secret exposure still requires credential rotation and coordinated Git-history cleanup.

## Reasonable future improvements

Add PostgreSQL container tests, end-to-end browser tests, password recovery/MFA, operational monitoring, and a transactional outbox only if reliable integration delivery becomes a real requirement.
