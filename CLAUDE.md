# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# Build
dotnet build

# Run (development)
dotnet run --project VacationPlannerWeb

# Add EF Core migration
dotnet ef migrations add <MigrationName> --project VacationPlannerWeb

# Apply migrations
dotnet ef database update --project VacationPlannerWeb
```

Development server runs on ports 5000 (HTTP) and 5001 (HTTPS). Uses SQL Server LocalDB (`VacationWebDB`) in development. No test project exists in this solution.

## Architecture

ASP.NET Core 10.0 MVC web application for managing employee vacation bookings with a manager approval workflow. Uses minimal hosting (`WebApplication.CreateBuilder()`) with top-level statements in `Program.cs`.

**Key flow:** User creates a vacation booking → booking is "Pending" → assigned manager approves/denies → calendar reflects status.

### Project Layout (under `VacationPlannerWeb/`)

- **Controllers/** — 6 MVC controllers. `VacationBookingsController` is the most complex, handling CRUD + approval workflows. `AccountController` handles Identity auth.
- **Models/** — Domain entities. `User` extends `IdentityUser` with Team, Department, and `ManagerUserId` (foreign key to approving manager).
- **ViewModels/** — View-specific models kept separate from domain models.
- **DataAccess/** — `AppDbContext` (inherits `IdentityDbContext<User, Role, string>`) and `DbInitializer` which seeds sample data on first run.
- **Services/** — `RolesService` for role lookups.
- **Extensions/** — `CalendarHelper` (date calculations) and `SessionExtensions` (JSON session storage).
- **Validations/** — Custom date validation attributes for booking date ranges (2-12 weeks in advance).
- **JsonModels/** — `SvenskaDagar` model for Swedish public holidays API integration.

### Auth & Roles

Uses ASP.NET Core Identity with cookie auth. Three roles with hierarchical permissions:
- **Admin** — full access to all users and bookings
- **Manager** — can approve/deny bookings for their assigned subordinates only (checked via `IsManagerForBookingUser()`)
- **User** — can create bookings and view own/team calendar

### Key Dependencies

- Entity Framework Core 10.0.5 (SQL Server provider)
- ASP.NET Core Identity
- Newtonsoft.Json 13.0.4 (for Swedish holidays API deserialization)

### Database Seeding

`DbInitializer` auto-seeds on startup: admin user (admin@gmail.com / Password123), manager, 5 users, teams, departments, absence types, and sample bookings.
