# lmsPortalBe

ASP.NET Core (net10.0) Web API backend for an LMS portal. SQLite via EF Core, JWT auth with HttpOnly refresh-token cookie.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Setup

1. Create your environment file:

   ```bash
   cp src/lmsPortalBe/.env.example src/lmsPortalBe/.env
   ```

2. Fill in `JWT_SECRET` (and optionally `ADMIN_USERNAME` / `ADMIN_EMAIL` / `ADMIN_PASSWORD` to seed an admin).

## Run

```bash
dotnet run --project src/lmsPortalBe
```

The API listens on `http://localhost:5250`. Migrations are applied and demo data is seeded automatically on startup.

## Test

```bash
dotnet test tests/lmsPortalBe.Tests/lmsPortalBe.Tests.csproj
```

## Project layout

- `src/lmsPortalBe` — Web API (controllers, EF Core models/migrations, services)
- `tests/lmsPortalBe.Tests` — xUnit integration tests