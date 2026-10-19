# Assignment & Submission Management System

A security-focused, role-based assignment platform for schools and colleges. Admins manage the academic structure, teachers work only within their allocated classes and subjects, and students see and submit only the work assigned to their class.

**Stack:** ASP.NET Core 10 · Entity Framework Core · PostgreSQL · React 19 · Vite · JWT bearer authentication · xUnit

## What makes this project strong

- Authorization is enforced in the API, including teacher allocation and student class boundaries.
- JWT signatures, issuer, audience, lifetime, and per-user token version are validated.
- Passwords use BCrypt with an explicit work factor; password or role changes revoke existing sessions.
- Login attempts are rate-limited and unknown-account checks use a dummy hash to reduce timing leakage.
- CORS accepts only configured frontend origins; HTTPS redirection, HSTS, security headers, bounded pagination, and centralized error responses are enabled.
- Request DTOs enforce email, password, text-length, marks, ID, and status constraints.
- Database constraints prevent duplicate accounts, allocations, submissions, classes, subjects, and settings.
- CI builds both applications, runs the backend regression suite, lints the frontend, and audits dependencies. NuGet advisories are treated as build errors; CodeQL and Dependabot are configured.

## Roles and enforced boundaries

| Role | Permissions |
|---|---|
| Admin | Manage users, classes, subjects, allocations, settings, assignments, and submissions |
| Teacher | View and manage assignments/submissions only for allocated class-subject pairs |
| Student | View published assignments for their class, submit their own work, and view only their submissions |

Submission states follow this workflow:

```text
UnderReview -> Graded
UnderReview -> Returned -> UnderReview
UnderReview -> Rejected
```

Graded and rejected work cannot be silently overwritten. Marks must be between zero and the assignment maximum.

## Project structure

```text
BackendApi/             ASP.NET Core API, EF Core model, and migrations
BackendApi.Tests/       Authorization and workflow regression tests
FrontendWeb/            React/Vite single-page application
.github/workflows/      CI and CodeQL security scanning
docs/SECURITY_AUDIT.md  Findings, fixes, remaining risks, and deployment checklist
.env.example            Environment-variable reference (never put real secrets here)
```

## Local setup

### Prerequisites

- .NET SDK 10
- Node.js 24 or newer
- PostgreSQL

### 1. Configure backend secrets

ASP.NET Core does **not** automatically import a local `.env` file. For development, use .NET User Secrets:

```powershell
cd BackendApi
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=SchoolDb;Username=postgres;Password=YOUR_DATABASE_PASSWORD"
dotnet user-secrets set "Jwt:Key" "YOUR_RANDOM_SIGNING_KEY_WITH_AT_LEAST_32_BYTES"
```

Generate a signing key with a cryptographically secure tool, for example:

```powershell
openssl rand -base64 48
```

For hosted environments, use the platform's secret manager and the variable names shown in `.env.example`.

### 2. Create the database schema

```powershell
dotnet ef database update
```

Database migration on application startup is disabled by default. This is safer for production and makes schema changes explicit.

### 3. Optional demo accounts

Demo seeding is opt-in and requires your own passwords:

```powershell
dotnet user-secrets set "SeedData:Enabled" "true"
dotnet user-secrets set "SeedData:AdminPassword" "YOUR_UNIQUE_ADMIN_PASSWORD"
dotnet user-secrets set "SeedData:TeacherPassword" "YOUR_UNIQUE_TEACHER_PASSWORD"
dotnet user-secrets set "SeedData:StudentPassword" "YOUR_UNIQUE_STUDENT_PASSWORD"
```

The seeded emails are `admin@school.com`, `teacher@school.com`, and `student@school.com`. Never enable demo seeding on a public deployment.

### 4. Run the API

```powershell
dotnet dev-certs https --trust
dotnet run --launch-profile https
```

- Swagger: `https://localhost:5001/swagger`
- Health check: `https://localhost:5001/health`

### 5. Run the frontend

Open another terminal:

```powershell
cd FrontendWeb
npm ci
npm run dev
```

Open `http://localhost:5173`. Vite proxies `/api` to the local HTTPS backend. For a separately hosted API, set `VITE_API_BASE_URL` at frontend build time and add the frontend origin to `Cors__AllowedOrigins`.

## Tests and quality checks

From the repository root:

```powershell
dotnet test BackendApi.Tests/BackendApi.Tests.csproj
cd FrontendWeb
npm ci
npm run lint
npm run build
npm audit
```

The backend suite covers role authorization, allocation boundaries, cross-class submission blocking, deadline enforcement, immutable graded work, marks validation, status validation, bounded pagination, and the complete submit-resubmit-grade workflow.

## API overview

```text
POST   /api/auth/login

GET    /api/assignments
GET    /api/assignments/{id}
POST   /api/assignments
PUT    /api/assignments/{id}
DELETE /api/assignments/{id}

GET    /api/submissions
GET    /api/submissions/my
POST   /api/submissions
PUT    /api/submissions/{id}/grade
PUT    /api/submissions/{id}/status

GET/POST/PUT/DELETE /api/users
GET/POST/PUT/DELETE /api/classes
GET/POST/PUT/DELETE /api/subjects
GET/POST/DELETE     /api/allocations
GET/POST            /api/settings
```

Swagger provides the complete request and response schemas. Protected calls require `Authorization: Bearer <token>`.

## Production checklist

- Read [the security audit](docs/SECURITY_AUDIT.md), especially the mandatory secret-rotation and Git-history steps.
- Set a unique random JWT key, database password, allowed host, and exact HTTPS frontend origin.
- Keep `SeedData__Enabled=false` and run migrations as a controlled deployment step.
- Put the API behind a trusted TLS reverse proxy and configure forwarded-header trust for that proxy.
- Use a managed PostgreSQL account with least privileges, encrypted backups, and network restrictions.
- Review GitHub CodeQL, Dependabot, and CI results before merging.

## Known product limitations

- Access tokens are short-lived and there is no refresh-token flow yet.
- Browser tokens use session storage; an HttpOnly same-site cookie design would provide stronger XSS resistance.
- Rate limiting is per application instance; multi-instance production should enforce limits at a gateway or shared store.
- File attachments, notifications, password recovery, MFA, and audit-event persistence are not implemented.

See [docs/SECURITY_AUDIT.md](docs/SECURITY_AUDIT.md) for the security boundary, corrected vulnerabilities, residual risk, and recommended roadmap.
