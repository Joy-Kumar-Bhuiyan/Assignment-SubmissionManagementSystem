# Interview guide

Use this to understand the repository, not as a script to memorize word for word.

## 1. Thirty-second explanation

This is a React and ASP.NET Core assignment platform with Admin, Teacher, and Student workflows. PostgreSQL and EF Core store data, while JWT claims plus database allocation/ownership checks enforce access. It also demonstrates audit history, role-aware reporting, SMTP/webhook integration, JSON/XML export, and meaningful regression tests.

## 2. Sixty-second explanation

Admins configure users, classes, subjects, and teacher allocations. A teacher can create and grade work only for allocated class-subject pairs. A student sees published work for their class and can only read or change their own non-terminal submission. The API validates JWTs, DTOs, deadlines, state transitions, marks, and pagination. Important changes create secret-free audit events; dashboard queries aggregate authorized data in the database. Optional email and webhooks are isolated behind interfaces so provider failure does not undo the core database action.

## 3. Three-minute explanation

Start with the problem and three roles. Trace a published assignment through student submission and teacher grading. Explain that server-side checks join the JWT identity to current Users, TeacherAllocations, Classes, Assignments, and Submissions; the frontend is only presentation. Then show dashboard aggregation, an Admin-filtered audit event, and XML export. Finish with tests/CI and honest limitations: EF InMemory instead of real PostgreSQL integration tests, no MFA/password recovery, browser session-storage tokens, and best-effort integrations.

## 4. Request flow

React calls a REST endpoint with JSON and a bearer token. ASP.NET Core authenticates the token, authorization attributes check the role, a controller validates the DTO, and `AccessControlService` applies object-level rules. EF Core translates LINQ `IQueryable` expressions into parameterized PostgreSQL SQL. The controller returns JSON (or the explicit XML export), and React updates the view.

## 5. Authentication

JWT authentication proves which account sent a request. The login endpoint verifies a BCrypt hash and issues claims for user ID, email, role, token ID, and token version. Middleware validates signature, issuer, audience, expiry, and required claims, then checks the current database user. Token lifetime is bounded; a role/password change increments `TokenVersion`, revoking older sessions.

## 6. Authorization

Authentication answers “who are you?” Authorization answers “may you perform this action?” Admin endpoints use role attributes. Teacher operations also require a matching TeacherAllocation. Student queries use the authenticated email/class and never accept a student email from the client. A hidden React button is not security because a caller can send HTTP requests directly.

## 7. OOP in this repository

- **Encapsulation:** `PasswordService` owns hashing/verification details; `NotificationService` owns provider-failure handling.
- **Abstraction:** `IEmailService` and `IOutgoingWebhookService` describe behavior without coupling controllers to SMTP or `HttpClient`.
- **Dependency injection:** `Program.cs` registers services and ASP.NET Core supplies them to constructors.
- **Polymorphism:** the same `IEmailService` reference can call `SmtpEmailService`, `LoggingEmailService`, or a test fake.
- **Inheritance:** controllers inherit `ControllerBase` and the test factory inherits `WebApplicationFactory`; there is no artificial domain inheritance hierarchy.

## 8. REST

Resources use predictable HTTP methods: GET reads, POST creates, PUT replaces/updates a known resource, PATCH changes submission status, and DELETE removes. Common results include 200, 400, 401, 403, 404, 409, and 429. Request DTOs prevent over-posting; JSON is the default representation.

## 9. EF Core

`AppDbContext` contains `DbSet` properties and relationship/index configuration. Migrations version schema changes. LINQ over `IQueryable` is translated to SQL when an async terminal method executes. `AsNoTracking` is used for read-only queries. Relationships connect users/classes, assignments/classes/subjects, submissions/assignments, and allocations/classes/subjects.

## 10. SQL

The application uses keys, foreign keys, uniqueness, indexes, joins generated from navigation queries, `COUNT`, grouping, and averages. See `MSSQL_PRACTICE.sql` for direct SQL Server syntax and one reporting procedure. Business writes remain in C#/EF Core rather than stored procedures.

## 11. PostgreSQL vs MSSQL

Both support relational modeling, transactions, joins, constraints, and EF Core. PostgreSQL remains the runtime provider because the existing application and migrations use Npgsql. SQL Server has different provider packages, migration annotations, T-SQL syntax, and operational tooling; supporting it properly would require separate verification.

## 12. XML

`IntegrationsController` projects EF entities to export DTOs, then uses `XmlSerializer` for `format=xml`. JSON remains normal. Serialization turns objects into XML; deserialization would do the reverse. This project does not parse incoming XML, so it does not enable DTD or external-entity resolution.

## 13. SOAP

SOAP exchanges XML envelopes containing headers and a body, often uses `SOAPAction`, and is described by WSDL. It commonly expresses errors as SOAP Faults. REST here uses resource routes, HTTP verbs, JSON, and status codes. Read `SOAP_INTEGRATION_NOTES.md`; do not claim a live SOAP integration.

## 14. Email integration

`IEmailService` is injected. SMTP is enabled only by configuration; otherwise a logging provider lets local development run. Messages intentionally omit answers/feedback/secrets. `NotificationService` catches provider failures after the core save, so assignment publication or grading remains successful.

## 15. Webhooks

A webhook is an outbound callback sent when an event happens; an API request is usually initiated by the consumer. The configured typed `HttpClient` sends a small JSON event and identifiers with a timeout. URLs are deployment configuration, not user input. A failed receiver is logged; delivery is not guaranteed because there is no durable outbox/retry worker.

## 16. Audit logging

CRM/SaaS systems need traceability for support and accountability. `AuditEvent` stores action, entity type/id, actor id/role, controlled description, and UTC time. Admins can filter it. It does not store passwords, tokens, submission answers, feedback, request bodies, or provider credentials and is not an immutable compliance ledger.

## 17. Security

Important controls are JWT validation, BCrypt, token versioning, rate limits, exact CORS origins, DTO bounds, server-side role/object authorization, safe configuration, centralized errors, HTTPS/security headers, query parameterization, and database constraints. Historical credentials must still be rotated and removed from Git history. Never say the application has no vulnerabilities.

## 18. Testing

Integration-style tests host the API with fake authentication and EF InMemory. They cover cross-role/class access, workflow rules, audit/reporting/export, filters, webhook JSON, and failure isolation. Small unit tests validate the webhook client. The main gap is no real PostgreSQL container test and no browser E2E suite.

## 19. CI

GitHub Actions restores/builds/tests .NET, installs/lints/builds React, and audits packages. CodeQL performs static security analysis. Dependabot proposes dependency updates. CI reduces regressions; it does not prove production safety.

## 20. If usage reached 100,000 users

Measure first. Add PostgreSQL query/slow-log monitoring, production indexes based on evidence, caching only for demonstrated read hotspots, background jobs plus a transactional outbox for reliable email/webhooks, object storage for files, distributed rate limiting, health/metrics/log aggregation, multiple stateless API instances behind a load balancer, and load tests. Split services only when team/deployment boundaries justify it.

# Likely questions and grounded answers

## Tell me about your project

- **Short answer:** It is a secure role-based assignment workflow with a React client and ASP.NET Core/PostgreSQL API.
- **Deeper answer:** It handles academic setup, teacher allocation, publication, submission, grading, audit history, reporting, and small integrations.
- **My project example:** A teacher can grade only submissions tied to an allocated class-subject pair.
- **File(s) to read:** `README.md`, `Controllers/AssignmentsController.cs`, `Controllers/SubmissionsController.cs`.

## Why ASP.NET Core?

- **Short answer:** It provides a strong typed Web API framework, middleware, dependency injection, authentication, and EF Core integration.
- **Deeper answer:** Those built-in pieces let this project keep security and request flow explicit without extra frameworks.
- **My project example:** `Program.cs` wires JWT validation, rate limiting, CORS, DI, Problem Details, and Swagger.
- **File(s) to read:** `BackendApi/Program.cs`.

## Why PostgreSQL?

- **Short answer:** It is a reliable open-source relational database with an excellent EF Core provider.
- **Deeper answer:** The data has strong relationships and uniqueness rules, so a relational database is natural.
- **My project example:** Npgsql migrations enforce one submission per student per assignment.
- **File(s) to read:** `BackendApi/Data/ApplicationDbContext.cs`, `BackendApi/Migrations/`.

## What is OOP, and where did you use it?

- **Short answer:** OOP groups state and behavior into types and uses boundaries such as encapsulation and interfaces.
- **Deeper answer:** I used services for cohesive behavior and interfaces only where implementations genuinely vary.
- **My project example:** `IEmailService` has SMTP, logging, and test-fake implementations; `PasswordService` encapsulates BCrypt.
- **File(s) to read:** `Services/EmailServices.cs`, `Services/PasswordService.cs`.

## What is dependency injection, and why use an interface for email?

- **Short answer:** DI supplies dependencies from configuration instead of classes constructing them directly; the interface decouples notification logic from SMTP.
- **Deeper answer:** This makes provider selection and testing simple without a service locator or static state.
- **My project example:** Tests replace `IEmailService` with `RecordingEmailService` and never send real mail.
- **File(s) to read:** `Program.cs`, `Services/EmailServices.cs`, `BackendApi.Tests/TestDoubles.cs`.

## What is a REST API?

- **Short answer:** It exposes resources over HTTP using standard methods, URLs, representations, and status codes.
- **Deeper answer:** REST is an architectural style, not simply “return JSON”; resource boundaries and stateless requests matter.
- **My project example:** `/api/assignments` uses GET/POST and `/api/assignments/{id}` uses GET/PUT/DELETE.
- **File(s) to read:** `Controllers/AssignmentsController.cs`.

## What are JSON and XML? JSON vs XML?

- **Short answer:** Both serialize structured data; JSON is compact and natural for web clients, while XML supports elements, attributes, namespaces, and many legacy contracts.
- **Deeper answer:** The React application uses JSON. XML is offered only on a deliberate export endpoint for interoperability.
- **My project example:** The same assignment export DTO returns JSON or valid XML with the correct content type.
- **File(s) to read:** `Controllers/IntegrationsController.cs`.

## What is SOAP? REST vs SOAP? How would you consume SOAP in C#?

- **Short answer:** SOAP is an XML messaging protocol with envelopes, WSDL-described contracts, SOAPAction, and Faults; REST here is resource-oriented HTTP.
- **Deeper answer:** I would generate a typed proxy or isolate an `HttpClient` client behind an interface, enforce timeouts, and parse XML with external entities disabled.
- **My project example:** SOAP is documented learning, not a production feature in this repository.
- **File(s) to read:** `docs/SOAP_INTEGRATION_NOTES.md`.

## What is JWT?

- **Short answer:** A JWT is a signed token containing claims that the API validates before trusting identity.
- **Deeper answer:** Signing prevents tampering; issuer, audience, expiry, and current database state also need validation.
- **My project example:** The token includes ID/email/role/token version, and middleware rejects stale role/password sessions.
- **File(s) to read:** `Controllers/AuthController.cs`, `Program.cs`.

## Authentication vs authorization?

- **Short answer:** Authentication proves identity; authorization decides permission.
- **Deeper answer:** A valid token is not enough to grade any submission; allocation and ownership rules still apply.
- **My project example:** `[Authorize(Roles="Teacher,Admin")]` plus `CanManageAssignmentAsync` protects grading.
- **File(s) to read:** `Services/AccessControlService.cs`, `Controllers/SubmissionsController.cs`.

## How do you stop one student seeing another submission, and why is frontend authorization insufficient?

- **Short answer:** The server derives the student email from the validated JWT and filters queries by it.
- **Deeper answer:** Client controls can be bypassed with curl/Postman or modified JavaScript, so the API must enforce every boundary.
- **My project example:** `/api/submissions/my` never accepts an email parameter, and create checks class access.
- **File(s) to read:** `Controllers/SubmissionsController.cs`, `BackendApi.Tests/SchoolSystemTests.cs`.

## How are passwords stored? What is BCrypt?

- **Short answer:** Passwords are stored only as salted BCrypt hashes, never plaintext.
- **Deeper answer:** BCrypt is deliberately expensive and has a configurable work factor; verification hashes the candidate against the stored hash.
- **My project example:** Work factor 12 is centralized, and unknown users get dummy verification to reduce timing differences.
- **File(s) to read:** `Services/PasswordService.cs`, `Controllers/AuthController.cs`.

## What is rate limiting?

- **Short answer:** It restricts request frequency to reduce abuse and resource exhaustion.
- **Deeper answer:** The login policy permits five attempts per minute per observed IP in one application instance.
- **My project example:** The sixth test login receives 429.
- **File(s) to read:** `Program.cs`, `BackendApi.Tests/SchoolSystemTests.cs`.

## What is CORS?

- **Short answer:** CORS tells browsers which origins may call the API from frontend JavaScript.
- **Deeper answer:** It is a browser control, not authentication; non-browser clients are unaffected.
- **My project example:** Only configured origins, methods, and Authorization/Content-Type headers are allowed.
- **File(s) to read:** `Program.cs`, `.env.example`.

## What is EF Core and a migration?

- **Short answer:** EF Core maps C# entities and LINQ to relational data; a migration versions a schema change.
- **Deeper answer:** Migrations must be reviewed and applied deliberately, especially when constraints or data movement are involved.
- **My project example:** `AddPortfolioFeatures` creates AuditEvents and four indexes.
- **File(s) to read:** `Data/ApplicationDbContext.cs`, `Migrations/*AddPortfolioFeatures.cs`.

## What are LINQ and IQueryable? IQueryable vs IEnumerable?

- **Short answer:** LINQ is C# query syntax; `IQueryable` builds a provider-translated query, while `IEnumerable` operates on objects already in memory.
- **Deeper answer:** Filtering an `IQueryable` before `ToListAsync` lets PostgreSQL do filtering/paging instead of loading all rows.
- **My project example:** assignment filters are applied before `CountAsync`, `Skip`, `Take`, and projection.
- **File(s) to read:** `Controllers/AssignmentsController.cs`, `Controllers/DashboardController.cs`.

## What are INNER JOIN and LEFT JOIN?

- **Short answer:** INNER JOIN keeps matches; LEFT JOIN keeps every left row and uses NULL for missing matches.
- **Deeper answer:** LEFT JOIN is how to find assignments with zero submissions or students who have not submitted.
- **My project example:** Both appear in the SQL Server practice queries over the real schema.
- **File(s) to read:** `docs/MSSQL_PRACTICE.sql`.

## What are GROUP BY, COUNT, and AVG?

- **Short answer:** GROUP BY creates groups; COUNT counts rows and AVG averages numeric values per group or query.
- **Deeper answer:** Aggregates should execute in the database and handle missing data/zero denominators carefully.
- **My project example:** dashboard submission statuses use GroupBy/Count, and graded percentages use AverageAsync.
- **File(s) to read:** `Controllers/DashboardController.cs`.

## What is an index?

- **Short answer:** An index speeds selected lookups/orderings but adds storage and write cost.
- **Deeper answer:** Add indexes for real query patterns, not every column.
- **My project example:** Audit filters justify indexes on date, action, actor, and entity pair.
- **File(s) to read:** `Data/ApplicationDbContext.cs`, latest migration.

## What is a stored procedure?

- **Short answer:** It is a named SQL program stored in the database.
- **Deeper answer:** It can be useful for reporting or controlled database operations, but moving all business rules into procedures would split this application's logic.
- **My project example:** The practice file contains one assignment summary procedure; runtime writes stay in EF Core.
- **File(s) to read:** `docs/MSSQL_PRACTICE.sql`.

## Why PostgreSQL instead of MSSQL? Could this project use SQL Server?

- **Short answer:** PostgreSQL was already the implemented provider; SQL Server is possible but needs its provider, migrations, and tests.
- **Deeper answer:** Shared SQL concepts transfer, but provider-specific types, annotations, SQL, and operational behavior must be verified.
- **My project example:** The application stays Npgsql; MSSQL knowledge is demonstrated honestly through schema-matched T-SQL.
- **File(s) to read:** `BackendApi.csproj`, `docs/SQL_INTERVIEW_GUIDE.md`.

## What is a webhook? Webhook vs normal API request?

- **Short answer:** A webhook is this server pushing an event to a configured receiver; a normal API call is usually a consumer requesting data/action from this server.
- **Deeper answer:** Webhooks reduce polling but introduce receiver outages, timeouts, authentication, retries, and duplicate-delivery concerns.
- **My project example:** publication/submission/grading send minimal ID-only events through a typed HttpClient.
- **File(s) to read:** `Services/WebhookServices.cs`, `Services/NotificationService.cs`.

## What happens if webhook or email delivery fails?

- **Short answer:** The core database action remains successful; the provider failure is logged safely.
- **Deeper answer:** This is best effort. Guaranteed delivery would need a transactional outbox/background worker and idempotent receiver.
- **My project example:** a test makes both providers throw and confirms grading remains `Graded`.
- **File(s) to read:** `Services/NotificationService.cs`, `BackendApi.Tests/PortfolioFeatureTests.cs`.

## What is an audit trail, and why have an audit table?

- **Short answer:** It is a chronological record of meaningful changes and actors.
- **Deeper answer:** It helps support investigations and accountability, but this mutable application table is not a tamper-proof compliance system.
- **My project example:** Admins filter `submission.graded` events by action, entity, actor, or date.
- **File(s) to read:** `Models/AuditEvent.cs`, `Controllers/AuditEventsController.cs`.

## What does your CI pipeline do?

- **Short answer:** It builds/tests .NET, lints/builds React, audits dependencies, and runs CodeQL.
- **Deeper answer:** Dependabot proposes updates; warnings for known NuGet advisories are treated as errors.
- **My project example:** backend and frontend jobs run independently on pushes and pull requests.
- **File(s) to read:** `.github/workflows/ci.yml`, `.github/workflows/codeql.yml`, `.github/dependabot.yml`.

## What tests are most important here?

- **Short answer:** Authorization tests are most important because data isolation is the highest-impact business risk.
- **Deeper answer:** Workflow, bounds, audit secrecy, role-scoped aggregates, integration failure, and serialization tests protect the next most important behavior.
- **My project example:** teacher-outside-allocation and student-other-class tests return 403.
- **File(s) to read:** `BackendApi.Tests/SchoolSystemTests.cs`, `PortfolioFeatureTests.cs`.

## What was the hardest part?

- **Short answer:** Keeping authorization correct across list, detail, grading, dashboard, export, and filter paths.
- **Deeper answer:** Every new query had to begin with the role/ownership scope so a filter could never widen access.
- **My project example:** Teacher dashboard and XML export repeat the same allocation boundary before aggregation/projection.
- **File(s) to read:** `AccessControlService.cs`, `DashboardController.cs`, `IntegrationsController.cs`.

## What security issue did you think about most?

- **Short answer:** Broken object-level authorization (IDOR/BOLA).
- **Deeper answer:** Role checks alone are insufficient; a teacher role still must not access another teacher's class, and a student must not select another identity or class.
- **My project example:** IDs are loaded and checked against allocation/class/claim data before mutation.
- **File(s) to read:** `docs/SECURITY_AUDIT.md`, `SchoolSystemTests.cs`.

## What would you improve before production?

- **Short answer:** Rotate historical secrets, clean history, test against real PostgreSQL, add cookie/CSRF or stronger token handling, recovery/MFA, monitoring, backups, and deployment hardening.
- **Deeper answer:** Also add durable delivery only if email/webhook guarantees become a requirement.
- **My project example:** These are explicitly listed rather than claimed as finished.
- **File(s) to read:** `README.md`, `docs/SECURITY_AUDIT.md`.

## What would you improve if the application became much larger?

- **Short answer:** Measure query/load behavior, scale the stateless API, add observability and background jobs, and evolve architecture only around demonstrated bottlenecks.
- **Deeper answer:** I would not begin with microservices; data ownership and team boundaries should justify a split.
- **My project example:** Current architecture intentionally keeps controllers, small services, EF Core, and PostgreSQL explainable.
- **File(s) to read:** `README.md`, `Program.cs`.
