# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TimeToLearn is a microservices-based educational platform. Backend services are ASP.NET Core 8.0 Web APIs; the frontend is a Vue 3 SPA.

## Build & Test Commands

### Backend

```bash
# Build all services
dotnet build TimeToLearn.sln

# Run all tests
dotnet test TimeToLearn.sln

# Run a single test project
dotnet test Backend/Authentication/Authentication.Tests/Authentication.Tests.csproj

# Run individual service
dotnet run --project Backend/Authentication/Authentication.API/Authentication.API.csproj
dotnet run --project Backend/Core/Core.API/Core.API.csproj
dotnet run --project Backend/Courses/Courses.API/Courses.API.csproj
dotnet run --project Backend/Forums/Forums.API/Forums.API.csproj
```

### Frontend

```bash
cd Frontend
npm run dev       # Dev server on port 3000 (proxies /api → http://acme.com)
npm run build     # Production build
npm run lint      # ESLint with auto-fix
npm run preview   # Preview production build
```

### Kubernetes

```bash
kubectl apply -f K8S/   # Deploy all services, databases, RabbitMQ, and ingress
```

## Architecture

### Services

| Service | Path | Responsibility |
|---------|------|----------------|
| Authentication | `Backend/Authentication/` | User registration, login, JWT issuance |
| Core (Users) | `Backend/Core/` | User profiles, universities, roles |
| Courses | `Backend/Courses/` | Course and lesson management |
| Forums | `Backend/Forums/` | Topics, comments, likes/dislikes |
| Frontend | `Frontend/` | Vue 3 SPA for all features |

### Inter-Service Communication

- **REST** — client-facing APIs via NGINX ingress (path-based routing: `/api/a` → Auth, `/api/u` → Core, `/api/c` → Courses, `/api/f` → Forums)
- **RabbitMQ** — async events: Authentication publishes `BaseUser_Published` on a fanout exchange named `"trigger"`; Core subscribes via a `MessageBusSubscriber` background service
- **gRPC** — synchronous sync: Courses and Forums call the Core service (port 666) to look up user info

### Layer Structure per Service

Each service follows the same three-layer pattern:
- `*.API` — controllers, middleware, migrations, infrastructure (RabbitMQ/gRPC wiring)
- `*.DAL` — EF Core `DbContext`, entity definitions, migrations
- `*.DL` — business logic services, repositories (generic `IBaseRepository<T>`)
- `*.Tests` — unit tests (XUnit/MSTest + Moq)

### Authentication

JWT Bearer with HS256. The same symmetric secret is shared across all backend services in `appsettings.json`. Password requirements: min 6 chars, uppercase, lowercase, digit, special char.

### Databases

Each service has its own MSSQL instance (database-per-service pattern):
- `auth_db` for Authentication, `users_db` for Core, separate DBs for Courses and Forums.
- Connection strings in `appsettings.json` use Kubernetes ClusterIP service names (e.g., `mssql-auth-clusterip-srv:1433`).

### Frontend Structure

```
Frontend/src/
├── Authentication/   login/register pages and auth API calls
├── Core/             universities and roles domain logic/UI
├── Courses/          courses domain (in progress)
├── Forums/           forums domain (in progress)
├── Users/            users domain (in progress)
└── Shared/           router, shared components, JWT utils, native fetch API client, global style system
```

Frontend conventions:
- Shared CSS belongs to `Frontend/src/Shared/styles/` and is imported once in `main.js`.
- Domain API files use the shared native fetch client in `Frontend/src/Shared/api/httpClient.js`.
- Vite dev proxy remains `/api -> http://acme.com`.
