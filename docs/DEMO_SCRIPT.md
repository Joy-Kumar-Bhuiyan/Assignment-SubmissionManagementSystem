# Recruiter demo script

## Three-minute version

**0:00–0:20 — Problem and stack**  
“This is a role-based assignment workflow built with React, ASP.NET Core, EF Core, and PostgreSQL. The API, not the UI, enforces every role and ownership boundary.”

**0:20–0:45 — Roles**  
Log in as Admin and briefly show users/classes/subjects/teacher allocations. Explain that allocations decide which class-subject pairs a teacher may manage.

**0:45–1:15 — Publish**  
Log in as the allocated Teacher. Create a draft, then publish it. Mention that publication writes an audit event, optionally emails students in that class, and sends an HMAC-signed webhook after the database save.

**1:15–1:40 — Submit**  
Log in as the Student in that class. Show that only published class assignments appear. Submit an answer. Mention deadline and terminal-state rules.

**1:40–2:05 — Grade**  
Return to Teacher, grade within the maximum, open `GET /api/submissions/{id}/history`, and show the Student result. Explain that every transition records the actor and UTC time, an unallocated teacher receives 403, and another student cannot access the submission.

**2:05–2:25 — Reporting and audit**  
In Swagger, call `GET /api/dashboard/summary` and `GET /api/audit-events?action=submission.graded`. Point out role-scoped LINQ aggregates and secret-free event descriptions.

**2:25–2:40 — Integration output**  
Call `GET /api/integrations/assignments/export?format=xml`. Show the XML content type and explain that JSON remains the normal format.

**2:40–3:00 — Quality boundary**  
Show the 30-test summary and CI workflow. Close with the honest limitations: EF InMemory tests, no MFA/password recovery, best-effort notification delivery, and required PostgreSQL/provider/deployment verification.

## Thirty-second version

“This is a React and ASP.NET Core assignment platform with Admin, Teacher, and Student workflows. PostgreSQL and EF Core store the data, while JWT claims plus database allocation and ownership checks enforce access in the API. The strongest backend pieces are the audited submission workflow, role-aware reporting, configurable email and outgoing webhook integrations, explicit JSON/XML export, database-side filtering, and regression tests for cross-user and cross-class access. It is a portfolio application, not a claimed production deployment, and its remaining risks are documented.”

## Before recording screenshots or video

Capture the login/role view, a published assignment, the Student submission screen, graded feedback, Swagger dashboard JSON, filtered audit events, XML export, and the green CI run. A 60–90 second GIF can help, but a narrated three-minute video shows authorization decisions better. Pin the repository and its README on the GitHub profile.
