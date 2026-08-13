# Assignment & Submission Management System

A role-based school/college assignment platform built for the OnnoRokom Projukti Limited Assistant Software Engineer recruitment project. It models a real academic workflow: admins set up the structure, teachers create and grade assignments, and students submit work and track their results — with every rule enforced on the backend, not just hidden in the UI.

**Stack:** ASP.NET Core Web API (C#) · React + Vite · PostgreSQL · JWT Auth

---

## Why I built it this way

The brief asked for more than CRUD — it asked for a system where roles actually matter and business rules can't be bypassed by hitting the API directly. So I focused on:

- Enforcing every permission check server-side (a hidden button isn't security)
- Modeling submissions as a real state machine, not a boolean flag
- Writing tests around the rules that actually matter — authorization, grading limits, deadlines
- Keeping the setup dead simple, so anyone can clone this and have it running in a few minutes

---

## Roles & Responsibilities

**Admin** — manages users, classes/courses, subjects, and assigns teachers to subjects/classes. Can view all assignments and submissions across the system.

**Teacher** — creates, updates, and deletes assignments; assigns each one to a specific class/course and subject; sets title, description, deadline, and max marks; publishes or keeps as draft; reviews submissions; assigns marks and feedback; updates submission status.

**Student** — views assignments for their own class/course; sees details and deadlines; submits an answer; updates a submission before the deadline (if allowed); views their own status, marks, and feedback.

```
Admin
  └─ users · classes · subjects · teacher allocation
        │
        ▼
     Teacher
        └─ create → draft → publish → review → grade
              │
              ▼
           Student
              └─ view → submit → update → check results
```

---

## Submission Workflow

A submission isn't just "submitted" — it moves through real states:

```
UnderReview → Graded
UnderReview → Returned → (student updates before deadline) → UnderReview
UnderReview → Rejected
```

This felt closer to how grading actually works — a teacher can send work back for revision instead of only accepting or rejecting it outright.

---

## Business Rules (enforced server-side)

- A student only sees assignments for their own class/course.
- A student can't access or edit another student's submission.
- A teacher can only manage assignments/submissions within their allocated class/subject.
- Marks can never exceed an assignment's maximum marks.
- Submission edits are blocked once the deadline passes.
- Every protected endpoint checks the caller's role and ownership before acting.

Try hitting a teacher-only endpoint in Swagger while logged in as a student — you'll get blocked exactly where you'd expect, because these checks live in the API, not the frontend.

---

## Assumptions

The brief left some details open to interpretation. Here's what I assumed, and why:

- **Frontend stack:** used **React + Vite (JavaScript)** instead of Next.js/TypeScript. I treated this as an equivalent technology per the brief's note that applicants may substitute suitable alternatives — Vite gave a faster iteration loop for a project this size, and I prioritized getting the role-based logic and backend rules right over the specific frontend framework. *(If TypeScript/Next.js is a hard requirement, happy to discuss or follow up with a migration.)*
- **Submission editing window:** a student can only update a submission while it's `UnderReview` or `Returned` — not after it's been `Graded` or `Rejected`, and never after the deadline.
- **Draft assignments:** a draft assignment is invisible to students entirely; only published assignments appear in a student's list.
- **Marks validation:** marks are validated against the assignment's max marks at the API level, regardless of what the frontend sends.
- **Class/course model:** modeled as a single `Class` entity tied to both students and subject allocations, rather than separating "class" and "course" as distinct concepts, since the brief uses them interchangeably.
- **File uploads:** a max file size setting (`MaxFileSizeMB`) is included in configuration to support assignment/submission attachments. *(Note: confirm and describe the actual current state of file upload support here based on what's implemented.)*

---

## Tech Stack

| Layer     | Technology                                          |
|-----------|-----------------------------------------------------|
| Frontend  | React, Vite, JavaScript, HTML5, CSS3                |
| Backend   | C#, ASP.NET Core Web API, Entity Framework Core     |
| Auth      | JWT, role-based authorization                       |
| Database  | PostgreSQL                                          |
| API Docs  | Swagger / OpenAPI                                   |
| Testing   | xUnit, FluentAssertions, EF Core In-Memory provider |

---

## Architecture

```
React (Vite)  →  HTTP/REST  →  ASP.NET Core Web API  →  EF Core  →  PostgreSQL
                                 (JWT + role checks)
```

---

## Project Structure

```
Assignment-Submission-Management-System/
├── BackendApi/          → Controllers, Services, Models, Migrations
├── FrontendWeb/          → React app (Vite)
├── BackendApi.Tests/     → xUnit test suite
├── .env.example           → required environment variables (see below)
├── start-app.bat         → runs both projects together
└── README.md
```

---

## Environment Variables

Copy `.env.example` and fill in your own local values — never commit real secrets.

```dotenv
# Server Port
PORT=5000

# PostgreSQL Database Connection String
ConnectionStrings__DefaultConnection="Host=localhost;Database=YourDatabase;Username=YourUsername;Password=YourPassword"

# JWT Token Security Settings
Jwt__Key="ChangeMeToAStrongRandomKey"
Jwt__Issuer="BackendApi"
Jwt__Audience="FrontendWeb"

# File Upload Settings
MaxFileSizeMB=10
```

For local development, these same values can also be set via **.NET User Secrets** instead of a real `.env` file:

```bash
cd BackendApi
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=YourDatabase;Username=YourUsername;Password=YourPassword"
dotnet user-secrets set "Jwt:Key" "YourLongRandomSecret"
```

---

## Running It Locally

**Prerequisites:** .NET SDK, Node.js, PostgreSQL, Git

```bash
dotnet --version
node --version
npm --version
git --version
```

### 1. Clone the repo
```bash
git clone https://github.com/Joy-Kumar-Bhuiyan/Assignment-SubmissionManagementSystem
cd Assignment-Submission-Management-System
```

### 2. Set up the database
Create a local PostgreSQL database matching your `.env` values, or use the defaults above.

### 3. Configure secrets
Use `.env.example` as a reference, or set up User Secrets as shown above.

### 4. Install & migrate
```bash
cd BackendApi
dotnet restore
dotnet ef database update
```
This creates the schema **and** runs the seed data — no manual table creation or data entry needed. Demo accounts for all three roles are seeded automatically (see below).

### 5. Run the backend
```bash
dotnet run
```
Swagger UI: `http://localhost:5000/swagger`

### 6. Run the frontend
```bash
cd ../FrontendWeb
npm install
npm run dev
```
Frontend: `http://localhost:5173`

> Or just run `start-app.bat` to launch both at once.

---

## API Reference

```
POST   /api/auth/login              → authenticate, returns JWT

GET    /api/assignments             → list (scoped by role)
POST   /api/assignments             → create (teacher only)
PUT    /api/assignments/{id}        → update (teacher only)
DELETE /api/assignments/{id}        → delete (teacher only)

GET    /api/submissions             → list (scoped by role)
POST   /api/submissions             → submit work (student)
PUT    /api/submissions/{id}        → update before deadline (student)
PUT    /api/submissions/{id}/grade  → grade + feedback (teacher only)
```

Every protected request requires:
```
Authorization: Bearer <token>
```

Full request/response schemas and live testing are available through Swagger.

---

## Demo Accounts

> ⚠️ Fill in your real seeded credentials here before submitting — the evaluator will log in with these directly.

| Role    | Email               | Password              |
|---------|---------------------|-----------------------|
| Admin   | admin@school.com    | AdminPassword123!     |
| Teacher | teacher@school.com  | TeacherPassword123!   |
| Student | student@school.com  | StudentPassword123!   |

These accounts are seeded automatically on `dotnet ef database update` / first run — no manual setup needed. Passwords are simple by design for local evaluation only and should never be reused in production.

---

## Testing

```bash
cd BackendApi.Tests
dotnet test
```

Tests focus on what actually breaks a system like this if it's wrong:
- Authentication & role-based authorization
- Assignment visibility rules per role
- Submission creation, updates, and deadline restrictions
- Grading rules and invalid-marks rejection
- Cross-role/cross-user access restrictions

Runs against EF Core's in-memory provider — no live database required to run the suite.

---

## Implementation Notes

- **UTC everywhere on the backend** — deadlines are stored and compared in UTC; the frontend converts to the user's local time for display. This avoids the classic bug where a deadline looks "already passed" the moment it's created due to timezone mismatch.
- **Backend validation is the real boundary** — the frontend validates for UX, but every rule that matters (ownership, deadlines, max marks) is re-checked server-side regardless of what the client sends.
- **CORS** — configured so the Vite dev server (`5173`) can talk to the API (`5000`) in local development.

---

## Known Limitations

- No email notifications or automated deadline reminders
- No refresh token flow — JWT expires and requires re-login
- No CI/CD pipeline or production deployment configuration
- Limited reporting/analytics (no dashboard beyond core role views)
- Pagination and advanced filtering not yet implemented

## Possible Future Improvements

- Assignment and submission file attachments (config for this already exists — see `.env.example`)
- Refresh token support
- Docker Compose for one-command setup
- Deployed live demo (Render/Railway) alongside the local setup
- Teacher/admin dashboard with basic analytics

---

## Security

No real secrets are committed to this repository — no passwords, JWT keys, or connection strings. Local configuration uses `.env` (via `.env.example` as a template) or `.NET User Secrets`. Before pushing to a public repository, this was verified with:

```bash
git grep -n -i -E "password|secret|apikey|api_key|client_secret|connectionstring|bearer"
```

---

Built as part of the Assistant Software Engineer recruitment process at OnnoRokom Projukti Limited — a hands-on exercise in role-based auth, REST API design, backend-enforced business rules, and full-stack integration between React and ASP.NET Core.