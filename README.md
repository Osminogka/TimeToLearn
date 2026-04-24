# TimeToLearn

TimeToLearn is a microservices-based educational platform with a Vue 3 single-page frontend and four backend ASP.NET Core services.

The system is built around one central idea:

- Authentication issues and refreshes JWT tokens.
- Core owns users, universities, and membership/role state.
- Courses and Forums own their own domain data.
- Courses and Forums delegate identity and permission checks to Core via gRPC.
- Frontend composes all domains into one user-facing application.

---

## Documentation Index

Use these links to quickly jump into the detailed docs for each bounded context and deployment layer:

- Authentication service: [Backend/Authentication/AUTHENTICATION.md](Backend/Authentication/AUTHENTICATION.md)
- Core service: [Backend/Core/CORE.md](Backend/Core/CORE.md)
- Courses service: [Backend/Courses/COURSES.md](Backend/Courses/COURSES.md)
- Forums service: [Backend/Forums/FORUMS.md](Backend/Forums/FORUMS.md)
- Frontend architecture: [Frontend/FRONTEND.md](Frontend/FRONTEND.md)
- Kubernetes manifests: [K8S](K8S)
- Repository engineering notes: [CLAUDE.md](CLAUDE.md)

---

## High-Level Architecture

```text
Frontend (Vue 3 SPA)
    |
    | HTTPS REST calls via /api/*
    v
NGINX ingress (path routing)
    |- /api/a -> Authentication service
    |- /api/u -> Core service
    |- /api/c -> Courses service
    |- /api/f -> Forums service

Authentication --(RabbitMQ: BaseUser_Published)--> Core
Courses ---------(gRPC user/university checks)----> Core
Forums ----------(gRPC user/university checks)----> Core
```

Main architecture style:

- Backend: distributed microservices with database-per-service.
- Communication: REST for client-facing APIs, gRPC for internal sync checks, RabbitMQ for async user replication events.
- Frontend: domain-first SPA with a shared kernel for routing, API client, auth/session, and styling.

---

## Backend Services Overview

### 1) Authentication Service

Primary responsibility:

- User registration and login.
- JWT issuance and refresh.
- OpenID/JWKS publishing for downstream token validation.
- Publishing user-created integration events to RabbitMQ.

What it stores:

- Identity users and credentials.

Key behavior:

- Issues RS256 JWTs with user name, email, and role claims.
- Exposes `/.well-known/openid-configuration` and `/.well-known/jwks.json`.
- On registration publishes `BaseUser_Published` so Core can create its user projection.
- Resolves role claim from Core, with a safe fallback when Core is unavailable.

Role in the platform:

- Security entry point and token authority.
- Source of authentication, but not source of university membership data.

---

### 2) Core Service

Primary responsibility:

- Central identity and membership domain for the platform.
- University lifecycle and governance rules.
- Role state transitions (teacher/student), teacher verification, invites/requests.
- gRPC provider for other services.

What it stores:

- Replicated platform users (`BaseUser`) from Authentication events.
- Universities and director ownership.
- Memberships (`StudentEnrollment`, `TeacherEnrollment`).
- Entry requests and invite workflow.

Key behavior:

- Consumes `BaseUser_Published` events from RabbitMQ.
- Exposes REST endpoints for profile, universities, invites, role transitions, member management.
- Exposes gRPC methods used by Courses and Forums:
  - membership/access check,
  - user name lookup,
  - university name lookup,
  - user role in university lookup.

Role in the platform:

- The authorization and membership source of truth across business services.

---

### 3) Courses Service

Primary responsibility:

- University-scoped course domain:
  - courses,
  - lessons,
  - lesson resources,
  - progress tracking,
  - grading,
  - quizzes.

What it stores:

- Course/lesson/quiz/progress data only.
- Does not store users/universities tables.

Key behavior:

- All reads/writes require permission context from Core gRPC.
- Teacher/director can create/update/delete and manage quiz content.
- Students can consume content, complete lessons, and submit quiz answers.
- Renders markdown lesson content via Markdig at read time.

Role in the platform:

- Learning content and assessment engine, isolated from identity ownership.

---

### 4) Forums Service

Primary responsibility:

- University-scoped discussion domain:
  - topics,
  - comments/replies,
  - likes/dislikes.

What it stores:

- Forum records and reaction data only.
- Does not store users/universities/roles.

Key behavior:

- Delegates access checks to Core over gRPC.
- Enriches forum responses with creator name and university role from Core.
- Supports paginated topic/comment retrieval and reaction toggles.

Role in the platform:

- Social discussion layer for each university workspace, separate from content management.

---

## How Services Communicate

### Client-facing communication: REST via ingress

- Frontend calls backend APIs through `/api/*`.
- Ingress routes each prefix to the corresponding service.
- This keeps one public entrypoint while preserving service boundaries.

### Internal synchronous communication: gRPC (Courses/Forums -> Core)

Courses and Forums call Core for identity and membership checks before allowing operations.

Typical flow for protected operations:

1. Request arrives with JWT.
2. Service validates token.
3. Service asks Core gRPC for user + university access context.
4. Service allows or rejects operation based on Core response.

This design centralizes authorization logic and avoids duplicating membership state.

### Internal asynchronous communication: RabbitMQ (Authentication -> Core)

- Authentication publishes `BaseUser_Published` on successful registration.
- Core consumes the event and creates/updates local user projection.
- This decouples signup from Core write path and supports eventual consistency.

---

## Authorization and Role Model (System-Wide)

Core concept:

- Authentication proves identity (who user is).
- Core determines domain authorization (what user can do in university context).

Role implications:

- Teacher/director permissions are enforced in domain services after Core membership checks.
- Student capabilities are restricted to learning/participation operations.
- Director operations (invites, member removal, university update) are handled by Core.

---

## Data Ownership and Boundaries

TimeToLearn follows database-per-service:

- Authentication DB: credentials and auth identities.
- Core DB: users projection, university structure, memberships, invites.
- Courses DB: course and assessment data.
- Forums DB: discussion and reaction data.

Why this matters:

- Clear ownership per domain.
- Independent schema evolution per service.
- Reduced tight coupling between business capabilities.

---

## Frontend Architecture (Vue 3 SPA)

The frontend is organized by domain-first folders with a shared kernel.

### Domain-first modules

- Authentication: login/register pages and auth APIs.
- Core: universities workspace and role setup.
- Courses: reusable course/lesson/markdown UI + APIs.
- Forums: forum topic/comment UI + APIs.
- Users: account management.

### Shared kernel (`src/Shared`)

Cross-domain infrastructure is centralized here:

- Router and route guards.
- HTTP client wrapper and API base paths.
- JWT/session utilities and role helper functions.
- Reusable UI components.
- Global style system (tokens/base/utilities/animations/forms).

### Frontend API strategy

- Every domain service uses one shared HTTP client.
- Service base paths are centralized:
  - auth: `/api/a/authentication`
  - users/core: `/api/u`
  - courses: `/api/c`
  - forums: `/api/f`

This avoids endpoint drift and gives consistent error handling.

### Route and access model

Router meta flags drive access behavior:

- `requiresAuth`
- `guestOnly`
- `requiresTeacher`
- `hidePrimaryNavigation`

Guards enforce redirects and role-based access consistently in one place.

### UI and rendering notes

- SPA entrypoint imports one global design system.
- Lesson markdown is rendered/sanitized in frontend services where applicable.
- Course and forum experiences are composed in university workspace pages.

---

## End-to-End Request Example

Example: teacher creates a lesson in a university course.

1. Frontend sends authenticated request to Courses endpoint.
2. Courses validates JWT.
3. Courses asks Core gRPC whether user can manage content for this university/course context.
4. If allowed, Courses writes lesson to its own DB.
5. Frontend refreshes course/lesson state via REST.

This pattern repeats across most protected business operations.

---

## Why This Architecture Works

Main strengths:

- Strong domain boundaries per service.
- Centralized authorization decisions in Core.
- Decoupled user provisioning via RabbitMQ events.
- Independent scalability and deployment of each service.
- Frontend mirrors backend bounded contexts while keeping shared concerns centralized.

In short, TimeToLearn is a modular, service-oriented platform where authentication, membership, learning content, and discussions are separated cleanly but integrated through well-defined communication contracts.

---

## Kubernetes Deployment Topology (`K8S/`)

The repository contains full Kubernetes manifests for application services, infrastructure services, storage claims, and ingress routing.

### What is deployed

Application deployments and services:

- `authenticationservice-depl.yaml`
  - Deploys `authservice` container (`ghcr.io/osminogka/authservice:latest`).
  - Exposes internal ClusterIP service `auth-clusterip-srv` on port 80 -> container 8080.
- `coreservice-depl.yaml`
  - Deploys `coreservice` container (`ghcr.io/osminogka/coreservice:latest`).
  - Exposes internal ClusterIP service `core-clusterip-srv` with:
    - HTTP port 80 -> 8080
    - gRPC port 666 -> 666
- `coursesservice-depl.yaml`
  - Deploys `coursesservice` container (`ghcr.io/osminogka/coursesservice:latest`).
  - Exposes internal ClusterIP service `courses-clusterip-srv` on port 80 -> 8080.
- `forumsservice-depl.yaml`
  - Deploys `forumsservice` container (`ghcr.io/osminogka/forumsservice:latest`).
  - Exposes internal ClusterIP service `forums-clusterip-srv` on port 80 -> 8080.
- `frontend-depl.yaml`
  - Deploys `frontend` container (`ghcr.io/osminogka/frontend:latest`).
  - Exposes internal ClusterIP service `frontend-clusterip-srv` on port 80.

Infrastructure deployments and services:

- `rabbitmq-depl.yaml`
  - Deploys RabbitMQ (`rabbitmq:3-management`).
  - Exposes:
    - ClusterIP `rabbitmq-clusterip-srv` for in-cluster communication.
    - LoadBalancer `rabbitmq-loadbalancer` for external access.
  - Ports:
    - 5672 (AMQP)
    - 15672 (management UI)
- `mssql-auth-depl.yaml`
- `mssql-core-depl.yaml`
- `mssql-courses-depl.yaml`
- `mssql-forums-depl.yaml`
  - Each deploys dedicated SQL Server 2019 Express instance.
  - Each exposes:
    - one ClusterIP service for in-cluster service-to-DB calls,
    - one LoadBalancer service for optional external DB access.
  - Uses Kubernetes Secret `mssql-secret` key `SA_PASSWORD`.
  - Mounts `/var/opt/mssql/data` to a dedicated PVC.

Storage:

- `local-pvc.yaml`
  - Defines one PVC per MSSQL instance:
    - `mssql-claim-auth`
    - `mssql-claim-core`
    - `mssql-claim-courses`
    - `mssql-claim-forums`
  - Each requests `200Mi`, `ReadWriteOnce`.

Traffic entrypoint:

- `ingress-srv.yaml`
  - NGINX ingress with host `acme.com`.
  - Path routing:
    - `/api/a` -> `auth-clusterip-srv`
    - `/api/u` -> `core-clusterip-srv`
    - `/api/c` -> `courses-clusterip-srv`
    - `/api/f` -> `forums-clusterip-srv`
    - `/` -> `frontend-clusterip-srv`

### Runtime communication inside Kubernetes

- Frontend traffic enters via ingress and is routed by path prefix.
- Backend services communicate with their own MSSQL instances via internal ClusterIP names.
- Authentication publishes events to RabbitMQ (`rabbitmq-clusterip-srv:5672`).
- Core consumes those events.
- Courses and Forums call Core gRPC endpoint on `core-clusterip-srv:666`.

### Deployment order (recommended)

1. Create secret for SQL SA password.
2. Apply storage claims (`local-pvc.yaml`).
3. Deploy SQL Server manifests.
4. Deploy RabbitMQ.
5. Deploy backend services.
6. Deploy frontend.
7. Apply ingress.

Example:

```bash
kubectl create secret generic mssql-secret --from-literal=SA_PASSWORD='Password123!'
kubectl apply -f K8S/
```

### Notes on current manifests

- All deployments are currently single replica (`replicas: 1`).
- Images use `:latest` + `imagePullPolicy: Always` for app services.
- DB services expose both internal and external endpoints (ClusterIP + LoadBalancer).
- One port mapping looks inconsistent: `mssql-auth-depl.yaml` and `mssql-courses-depl.yaml` both expose LoadBalancer port `1435`; if both run in the same cluster/network and need distinct external ports, one should be changed.
