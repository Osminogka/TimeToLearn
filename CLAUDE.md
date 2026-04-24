# CLAUDE.md

## 1. Project Overview

- TimeToLearn is a microservices educational platform with a Vue 3 SPA frontend and ASP.NET Core backend services.
- Public traffic enters through NGINX ingress and is routed by path prefix.
- Domain ownership is explicit and separated by database-per-service.

Services and responsibilities:

- Authentication (`Backend/Authentication`): registration, login, refresh token, JWT issuance, OpenID/JWKS publishing, `BaseUser_Published` event publishing.
- Core (`Backend/Core`): source of truth for users projection, universities, memberships, role transitions, invites/requests; gRPC provider for authorization context.
- Courses (`Backend/Courses`): course/lesson/resource/progress/grading/quiz domain; relies on Core gRPC for identity and access checks.
- Forums (`Backend/Forums`): topics/comments/replies/reactions domain; relies on Core gRPC for identity and access checks.
- Frontend (`Frontend`): domain-first Vue SPA that composes all service APIs via shared HTTP client and route guards.

## 2. Architecture Rules

Service boundaries:

- Each backend service owns only its own DB schema and migrations.
- Do not read/write another service DB directly.
- Core owns university membership and authorization semantics.
- Authentication owns token issuance and auth identity lifecycle.
- Courses and Forums own content/discussion data only.

Communication rules:

- Client-facing: REST via ingress paths only:
  - `/api/a` -> Authentication
  - `/api/u` -> Core
  - `/api/c` -> Courses
  - `/api/f` -> Forums
- Internal sync: gRPC only from Courses/Forums to Core (membership/user/university context).
- Internal async: RabbitMQ event `BaseUser_Published` from Authentication to Core.

Allowed cross-service interactions:

- Authentication -> Core via HTTP role lookup client.
- Authentication -> RabbitMQ publish user-created integration event.
- Core <- RabbitMQ consume and project users.
- Courses -> Core gRPC for access checks and identity lookups.
- Forums -> Core gRPC for access checks and identity lookups.

Forbidden cross-service interactions:

- Courses/Forums writing role or membership data locally.
- Any service using another service DbContext or connection string.
- Frontend bypassing service base paths or calling cluster-internal hosts directly.
- Silent contract changes in DTOs/proto/events without coordinated consumer updates.

## 3. Service-Specific Guidelines

### Authentication

Responsibility:

- Account registration/login/refresh.
- JWT issuance and OpenID/JWKS endpoints.
- User-created event publishing for Core projection.

Key modules/files:

- `Backend/Authentication/Authentication.API/Program.cs`
- `Backend/Authentication/Authentication.API/Controllers/AuthenticationController.cs`
- `Backend/Authentication/Authentication.API/Controllers/WellKnownController.cs`
- `Backend/Authentication/Authentication.API/AsyncDataService/MessageBusClient.cs`
- `Backend/Authentication/Authentication.API/Infrastructure/RsaKeyService.cs`
- `Backend/Authentication/Authentication.API/Infrastructure/CoreRoleClient.cs`
- `Backend/Authentication/Authentication.DL/Services/AuthService.cs`

Do not modify casually:

- JWT claims/issuer/signing setup.
- OpenID discovery/JWKS payload shape.
- Event name `BaseUser_Published` and published DTO fields.
- Password policy and Identity setup.

Common patterns:

- Controller returns `ResponseMessage` wrappers.
- Business logic in DL services; persistence via repository abstractions.
- Event publish failures are logged and should not corrupt successful registration flow.

### Core

Responsibility:

- Users projection from auth events.
- University lifecycle and member management.
- Role transitions and verification workflows.
- gRPC identity/authorization context provider.

Key modules/files:

- `Backend/Core/Core.API/Program.cs`
- `Backend/Core/Core.API/EventProcessing/EventProcessor.cs`
- `Backend/Core/Core.API/AsyncDataService/MessageBusSubscriber.cs`
- `Backend/Core/Core.API/Grpc/GrpcUserinfoService.cs`
- `Backend/Core/Core.API/Proto/userinfo.proto`
- `Backend/Core/Core.DL/Services/*Service.cs`
- `Backend/Core/Core.DAL/Context/DataContext.cs`
- `Backend/Core/Core.DAL/Models/*`

Do not modify casually:

- Enrollment model semantics (`StudentEnrollment`, `TeacherEnrollment`).
- gRPC response contracts used by Courses/Forums.
- Event consumer mapping logic for `BaseUser_Published`.
- University permission logic (director/teacher/student checks).

Common patterns:

- Generic repository `IBaseRepository<T>` used by DL services.
- Controllers delegate to DL services; exception handling mapped in base controller.
- gRPC service is the canonical cross-service authorization API.

### Courses

Responsibility:

- Course and lesson management.
- Lesson resources, progress, grading.
- Lesson and course quiz lifecycle and submissions.

Key modules/files:

- `Backend/Courses/Courses.API/Program.cs`
- `Backend/Courses/Courses.API/Controllers/CourseController.cs`
- `Backend/Courses/Courses.API/Controllers/LessonController.cs`
- `Backend/Courses/Courses.API/Controllers/QuizController.cs`
- `Backend/Courses/Courses.DL/Grpc/UserInfoClient.cs`
- `Backend/Courses/Courses.DL/Services/CourseService.cs`
- `Backend/Courses/Courses.DL/Services/LessonService.cs`
- `Backend/Courses/Courses.DL/Services/ProgressService.cs`
- `Backend/Courses/Courses.DL/Services/QuizService.cs`
- `Backend/Courses/Courses.DAL/Context/DataContext.cs`

Do not modify casually:

- Authorization gate ordering (JWT validation first, Core gRPC check before writes).
- `TeacherId`/`UniversityId` semantics (external IDs from Core, not local FK ownership).
- Quiz submission correctness logic and attempt policy behavior.
- Markdown rendering/sanitization behavior coupling with frontend rendering.

Common patterns:

- Read/write authorization driven by `UserInfoForCourse` from Core gRPC.
- Repository abstraction for all EF operations.
- Response wrappers (`ResponseMessage`, `ResponseArray`, `ResponseWithValue`).

### Forums

Responsibility:

- Topics/comments/replies.
- Like/dislike toggles.
- Forum read model enrichment with Core user/role data.

Key modules/files:

- `Backend/Forums/Forums.API/Program.cs`
- `Backend/Forums/Forums.API/Controllers/TopicController.cs`
- `Backend/Forums/Forums.API/Controllers/CommentController.cs`
- `Backend/Forums/Forums.DL/Grpc/UserInfoClient.cs`
- `Backend/Forums/Forums.DL/Services/TopicService.cs`
- `Backend/Forums/Forums.DL/Services/CommentService.cs`
- `Backend/Forums/Forums.DAL/Context/DataContext.cs`

Do not modify casually:

- Topic/comment pagination behavior and page-size assumptions.
- Reaction toggle consistency logic (like vs dislike exclusivity).
- gRPC-based access validation before content visibility or writes.

Common patterns:

- Service-level access checks with Core gRPC even where controller allows anonymous read.
- Repository pattern and response wrappers.
- Defensive handling of gRPC failures as authorization/data failure paths.

## 4. Frontend Guidelines

Backend interaction model:

- Always call APIs through `Frontend/src/Shared/api/httpClient.js`.
- Use `Frontend/src/Shared/api/serviceBasePaths.js` as the only source for service roots.
- Do not hardcode hostnames, service URLs, or duplicate base paths in feature modules.

API usage patterns:

- Domain services only compose endpoints and payloads.
- Auth-protected calls use `getAuth/postAuth/putAuth/deleteAuth` helpers.
- Preserve existing error normalization through shared `ApiError` handling.

State and navigation expectations:

- JWT and user identity state are centralized in `Frontend/src/Shared/services/utils.js`.
- Route access logic is centralized in `Frontend/src/Shared/router/index.js` route meta and guards.
- Keep role-based redirects and navigation visibility consistent with existing route meta flags.

UI/data consistency constraints:

- Preserve current domain-first structure: `Authentication`, `Core`, `Courses`, `Forums`, `Users`.
- Reuse shared components/styles before introducing new primitives.
- Keep list pagination and role-gated UI behavior aligned with backend response semantics.

## 5. Development Rules for Claude

- Never guess API contracts; locate controllers, DTOs, proto files, and existing callers first.
- Prefer extending existing service/repository/controller patterns over introducing new abstractions.
- Reuse existing response wrappers and exception mapping style.
- Keep changes minimal, localized, and scoped to one bounded context unless contract changes require coordinated updates.
- Preserve architecture invariants: database-per-service, Core as authz authority, gRPC and event contracts as integration boundaries.
- When changing shared contracts (DTO/proto/event), update all producers and consumers in the same change.
- Verify with targeted builds/tests for affected projects before broad solution-wide runs.

## 6. Code Modification Policy

Refactor only when:

- The current structure blocks required behavior.
- There is repeated logic within the same service that can be consolidated safely.
- Contract behavior remains unchanged unless explicitly requested.

Do not refactor when:

- Change is cosmetic or broad and unrelated to the task.
- It introduces cross-service coupling or contract drift.
- It requires migration of unrelated modules without a clear requirement.

Safe extension rules:

- Add new endpoints inside the owning service only.
- Reuse existing DTO/response patterns and validation approach.
- Add or update EF migrations only in the owning service DAL/API migration flow.
- For new cross-service needs, prefer adding explicit gRPC methods in Core over duplicating Core data elsewhere.

Rules for new endpoints/services:

- Endpoint path must follow existing ingress prefix and controller conventions.
- Auth and authorization must match service rules (JWT + Core gRPC where required).
- New service introduction must define DB ownership, ingress route, and integration contracts explicitly.

## 7. Common Workflows

Adding a new feature:

1. Identify owning bounded context and service.
2. Locate existing controller/service/repository path for similar behavior.
3. Add DTO/model changes in owning service only.
4. Implement business logic in DL service, then expose via API controller.
5. If frontend is needed, add/extend domain service API module and wire UI route/component.
6. Add/update targeted tests in affected backend test project and frontend unit/integration tests if present.

Modifying an existing service:

1. Trace entrypoint controller -> DL service -> repository -> EF model.
2. Check external contracts (gRPC, events, DTOs) before edits.
3. Keep method signatures and response wrappers stable unless change is required.
4. Validate with project build and targeted tests.

Debugging issues across services:

1. Confirm ingress route and frontend base path mapping.
2. Validate JWT/auth path first (Authentication/OpenID/JWKS).
3. For Courses/Forums permission issues, inspect Core gRPC responses and membership data.
4. For user projection inconsistencies, inspect RabbitMQ publish/consume path and Core event processing.
5. Verify per-service DB state in the owning schema only.

## 8. Anti-Patterns to Avoid

- Cross-service DB access or shared-table assumptions.
- Copying Core authorization logic into Courses/Forums.
- Introducing alternate HTTP clients or duplicated API base path constants in frontend.
- Silent changes to gRPC proto, event names, or DTO field semantics.
- Mixing unrelated refactors with feature/bugfix changes.
- Returning inconsistent response shapes from established API surfaces.
- Bypassing shared auth/session helpers or router guard conventions in frontend.
