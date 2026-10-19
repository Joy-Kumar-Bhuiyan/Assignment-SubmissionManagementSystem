# Final portfolio review

These scores are intentionally conservative. They describe repository evidence, not production experience.

| Area | SaaS/CRM backend intern | Junior C#/.NET | Reason when below 8 |
|---|---:|---:|---|
| C# | 8 | 8 | — |
| OOP | 8 | 8 | — |
| ASP.NET Core | 8 | 8 | — |
| REST | 8 | 8 | — |
| JSON | 8 | 8 | — |
| XML | 7 | 7 | One focused export exists; no incoming XML contract or schema validation. |
| SOAP familiarity | 6 | 7 | Documented knowledge only; no live or coded SOAP client. |
| PostgreSQL | 8 | 7 | Strong EF schema/migrations, but no real-database automated tests or query-plan evidence. |
| MSSQL familiarity | 6 | 7 | Schema-matched T-SQL and one procedure, but SQL Server is not an application provider. |
| SQL | 8 | 8 | — |
| Authentication | 8 | 8 | — |
| Authorization | 9 | 9 | — |
| Security fundamentals | 8 | 8 | — |
| Third-party integration | 8 | 8 | — |
| Testing | 8 | 8 | — |
| Error handling | 8 | 8 | — |
| Git/GitHub | 8 | 8 | — |
| CI | 8 | 8 | — |
| Maintainability | 8 | 8 | — |
| Documentation | 9 | 9 | — |
| Demo quality | 8 | 8 | — |
| Interview readiness | 9 | 9 | — |

**Overall Credosis/SaaS-CRM fit: 8.2/10.** The strongest evidence is REST, SQL-backed role scoping, audit/reporting, email/webhooks, security checks, and tested failure behavior.

**Overall Junior .NET fit: 8.3/10.** The strongest evidence is C#/ASP.NET Core, DI/OOP, EF Core/LINQ, JWT authorization, tests, XML export, and honest SQL Server/SOAP learning artifacts.

## Top five strengths

1. Object-level authorization is tested, not only described.
2. The complete Admin → allocation → Teacher → Student → grading workflow is coherent.
3. Audit and dashboard features use real EF Core queries and a real migration.
4. Email/webhook boundaries are small, configurable, mockable, and failure-tolerant.
5. Documentation clearly separates implemented features, examples, and limitations.

## Top five weaknesses

1. Tests use EF InMemory rather than an ephemeral PostgreSQL database.
2. There is no browser E2E suite or published deployment.
3. SOAP is knowledge documentation, not a working external-service client.
4. MSSQL is practice SQL rather than a verified second provider.
5. Email/webhooks are best effort without a durable outbox/retry path.

## Five things a SaaS/CRM interviewer may like

1. Admin-only audit history with operational filters.
2. Role-aware reporting and database-side aggregation.
3. Email and webhook integrations behind testable interfaces.
4. Clear failure isolation and honest delivery semantics.
5. Security-aware API boundaries and maintainable monolith scope.

## Five things a .NET interviewer may like

1. Constructor injection and modest interfaces with real polymorphism.
2. EF Core model configuration, migrations, LINQ, and async queries.
3. JWT middleware plus resource-level authorization.
4. JSON/XML output with DTO projection.
5. xUnit integration tests and schema-matched T-SQL practice.

## Ten likely technical questions

1. How does a teacher's allocation restrict both list and mutation endpoints?
2. How do you stop one student from changing another student's submission?
3. Why does token versioning revoke an old JWT?
4. Why are filters applied before `ToListAsync`?
5. How is average marks percentage calculated without loading every row?
6. Why use interfaces for email/webhooks but not a repository over EF Core?
7. What happens after an SMTP or webhook failure?
8. How is the webhook URL protected from user-controlled SSRF?
9. Why is XML an export only, and how would secure XML parsing differ?
10. What would you add before calling this production-ready?

## Features to remove

None of the new runtime features look overengineered. Do not add a SOAP server/client without a real service, a second EF Core database provider just for a keyword, CQRS/MediatR, microservices, Redis, or a message broker at the current scale. The most removable artifact is only the documentation-only MSSQL practice file if a recruiter expects exclusively executable production code; otherwise it is clearly labeled and useful for the target role.
