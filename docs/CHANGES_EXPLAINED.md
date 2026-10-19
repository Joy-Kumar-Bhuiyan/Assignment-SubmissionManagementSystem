# Changes explained

## Persistent audit events

**What:** `AuditEvent` stores important user, academic-structure, assignment, submission, allocation, and setting changes. Admins can query a paginated, filtered history.

**Why:** SaaS and CRM-style systems need traceability, and this is a concrete EF Core data-model/query example.

**How:** Controllers add short controlled descriptions through `AuditService`; EF Core persists them in `AuditEvents`. Indexes support date, action, actor, and entity lookups.

**Security considerations:** the endpoint is Admin-only. Descriptions never use passwords, tokens, request bodies, answer text, feedback, or configuration secrets. This is an application audit trail, not an immutable compliance log.

**Interview explanation:** “After an authorized state change I write a small event that says who did what, to which entity, and when. I deliberately do not store secret or full request data.”

**Files to study:** `Models/AuditEvent.cs`, `Services/AuditService.cs`, `Controllers/AuditEventsController.cs`, `Data/ApplicationDbContext.cs`.

## Role-aware dashboard

**What:** `GET /api/dashboard/summary` returns different database-derived counts for Admin, Teacher, and Student.

**Why:** it demonstrates LINQ aggregation, SQL thinking, and authorization beyond basic CRUD.

**How:** every role begins with an authorized `IQueryable`; `CountAsync`, `GroupBy`, and `AverageAsync` execute in the database. Teacher data is constrained by allocations and student data by identity/class.

**Security considerations:** no client-supplied identity chooses the scope. The JWT claims and current database relationships define it.

**Interview explanation:** “The query is narrowed for the signed-in role before aggregation, so the summary cannot count records the role should not see.”

**Files to study:** `Controllers/DashboardController.cs`, `Services/AccessControlService.cs`.

## Email notifications

**What:** assignment publication, grading, and return-for-revision can send short emails.

**Why:** it demonstrates dependency injection and a small third-party integration.

**How:** `IEmailService` has SMTP and disabled logging implementations. `NotificationService` selects recipients and catches provider failures after the core database save.

**Security considerations:** credentials come only from configuration; messages exclude answers, marks, feedback, tokens, and secrets. Tests replace SMTP with a recording fake.

**Interview explanation:** “The interface lets production use SMTP and tests use a fake. If SMTP is down, grading stays successful and the failure is logged.”

**Files to study:** `Services/EmailServices.cs`, `Services/NotificationService.cs`, `Program.cs`.

## Outgoing webhook

**What:** configured receivers can receive `assignment.published`, `submission.created`, and `submission.graded` JSON events.

**Why:** it shows server-to-server HTTP integration with `HttpClientFactory`.

**How:** one typed client posts a minimal event, UTC timestamp, and identifiers with a 1–30 second timeout.

**Security considerations:** the URL is trusted deployment configuration, never request/admin input. Production requires HTTPS. Payloads exclude student answers and secrets. Delivery is best effort and has no retry queue.

**Interview explanation:** “A webhook lets this server notify another server when an event occurs. It is not the same as that server polling our API.”

**Files to study:** `Services/WebhookServices.cs`, `Services/NotificationService.cs`, `Program.cs`, `WebhookServiceTests.cs`.

## JSON/XML export

**What:** one endpoint exports role-scoped assignment DTOs as JSON or XML.

**Why:** normal APIs stay JSON while the project demonstrates legacy/enterprise interoperability.

**How:** EF projects directly to DTOs. `System.Xml.Serialization.XmlSerializer` writes XML; no incoming XML is parsed.

**Security considerations:** role scoping is applied before projection. Because there is no XML parser, XXE is not exposed by this feature.

**Interview explanation:** “JSON is normal for the React client. XML is an explicit export format rather than a second application architecture.”

**Files to study:** `Controllers/IntegrationsController.cs` and its tests.

## Search, filters, and sorting

**What:** assignment, submission, user, and audit lists accept explicit bounded filters and validated sort fields.

**Why:** this makes the APIs useful and demonstrates safe query composition.

**How:** filters are appended to `IQueryable` before `CountAsync`, `Skip`, `Take`, and materialization.

**Security considerations:** filtering never replaces role/ownership predicates; sort names come from a fixed allowlist rather than dynamic SQL.

**Files to study:** `AssignmentsController.cs`, `SubmissionsController.cs`, `UsersController.cs`, `AuditEventsController.cs`.
