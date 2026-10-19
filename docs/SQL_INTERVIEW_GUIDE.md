# SQL interview guide

All examples refer to this repository's schema.

- **Primary key:** a unique row identifier, such as `Assignments.Id`.
- **Foreign key:** a relationship guard. `Submissions.AssignmentId` must point to an assignment. EF configures cascade deletion for this required relationship.
- **Unique constraint/index:** prevents duplicates even when two requests race. Examples are `Users.Email`, `Subjects.Code`, and the pair `Submissions(AssignmentId, StudentEmail)`.
- **Index:** an extra data structure that speeds a known lookup or ordering pattern at the cost of storage and write work. Audit indexes match filters by action, actor, entity, and date.
- **INNER JOIN:** returns only matching rows. Joining Assignments to Subjects omits assignments without a subject.
- **LEFT JOIN:** keeps every left-side row. It is useful for “assignments with zero submissions.”
- **GROUP BY:** combines rows into groups so `COUNT` or `AVG` can summarize each assignment, class, or subject.
- **HAVING:** filters groups after aggregation; `HAVING COUNT(s.Id) >= 5` keeps assignments with at least five submissions.
- **Stored procedure:** named SQL stored by the database. `MSSQL_PRACTICE.sql` includes one read-only reporting example; core business rules stay in C#.
- **Transaction:** a set of operations committed or rolled back together. EF Core wraps one `SaveChangesAsync` call in a transaction for relational providers.
- **Normalization:** separating Users, Classes, Subjects, Assignments, and Submissions reduces duplicate data and update inconsistency. The schema still stores teacher/student emails in allocation/submission rows for compatibility; numeric user foreign keys would be a future cleanup.

## EF Core and SQL

`AppDbContext` maps C# entities to tables. `DbSet<T>` is the query starting point. LINQ over `IQueryable` builds an expression tree; Npgsql translates it to parameterized PostgreSQL SQL when methods such as `ToListAsync`, `CountAsync`, or `AverageAsync` execute. This is why filters are applied before pagination and why user input is not concatenated into SQL.

`AsNoTracking` avoids change-tracking overhead for read-only queries. Migrations version schema changes; they must be reviewed and applied in staging before production.

## PostgreSQL and SQL Server

Both are relational databases with keys, joins, transactions, indexes, and strong EF Core providers. This project keeps PostgreSQL because it is already implemented and tested. SQL Server often uses `TOP`, `OFFSET ... FETCH`, `GETUTCDATE`/`SYSUTCDATETIME`, square-bracket identifiers, T-SQL stored procedures, and Microsoft deployment tooling. PostgreSQL commonly uses `LIMIT/OFFSET`, double-quoted identifiers, `NOW()`, and PL/pgSQL functions. Provider-specific migrations and SQL syntax mean switching is not only a connection-string change.

Read [MSSQL_PRACTICE.sql](MSSQL_PRACTICE.sql) alongside `ApplicationDbContext.cs` and the latest migration.
