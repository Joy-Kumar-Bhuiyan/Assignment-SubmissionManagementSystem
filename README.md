# Assignment & Submission Management System

A role-based school application built with ASP.NET Core Minimal APIs, PostgreSQL, and React.

## Demo Credentials
- **Admin**: admin@school.com / AdminPassword123!
- **Teacher**: teacher@school.com / TeacherPassword123!
- **Student**: student@school.com / StudentPassword123!

## Technology Stack
- **Backend**: .NET 10 Web API, Entity Framework Core, JWT Bearer Auth
- **Database**: PostgreSQL
- **Frontend**: React (Vite)
- **Testing**: xUnit

## Setup & Execution

### Prerequisites
- .NET 10 SDK
- Node.js (v18+)
- PostgreSQL

### Backend Setup
1. Open `BackendApi/appsettings.json` and set your PostgreSQL password.
2. Run database migrations:
   ```bash
   cd BackendApi
   dotnet ef database update
   dotnet run