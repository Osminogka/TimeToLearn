# Core Service

Core is the identity and membership domain service for TimeToLearn.
It stores platform users, manages university lifecycle and membership, processes user-created events from Authentication, and exposes gRPC methods consumed by Courses and Forums.

---

## What This Service Handles

Core owns these responsibilities:

- Persisting platform users (`BaseUser`) replicated from Authentication events.
- Role state for users (`teacher` vs `student`) with teacher verification.
- University management (create, list, update, member visibility).
- Membership management through explicit enrollment tables:
  - `StudentEnrollment`
  - `TeacherEnrollment`
- University invite/request workflow (`EntryRequest`) for closed/open universities.
- Director operations: invite, remove members, update university data.
- gRPC identity/membership lookups for other backend services.

---

## Current Project Structure

```text
Core/
├── Core.API/
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Controllers/
│   │   ├── BaseController.cs
│   │   ├── BaseUserController.cs
│   │   ├── StudentController.cs
│   │   ├── TeacherController.cs
│   │   ├── DirectorController.cs
│   │   ├── UniversityController.cs
│   │   └── GeneralinfoController.cs
│   ├── AsyncDataService/
│   │   └── MessageBusSubscriber.cs
│   ├── EventProcessing/
│   │   ├── IEventProcessor.cs
│   │   └── EventProcessor.cs
│   ├── Grpc/
│   │   └── GrpcUserinfoService.cs
│   ├── Infrastructure/
│   │   └── Mapper.cs
│   └── Proto/
│       └── userinfo.proto
├── Core.DL/
│   ├── Repositories/
│   │   ├── IBaseRepository.cs
│   │   └── BaseRepository.cs
│   └── Services/
│       ├── BaseUserService.cs
│       ├── StudentService.cs
│       ├── TeacherService.cs
│       ├── DirectorService.cs
│       ├── UniversityService.cs
│       ├── GeneralUserInfoService.cs
│       └── service interfaces (I*Service)
├── Core.DAL/
│   ├── Context/
│   │   └── DataContext.cs
│   ├── Models/
│   │   ├── BaseEntity.cs
│   │   ├── Address.cs
│   │   ├── BaseUser.cs
│   │   ├── Teacher.cs
│   │   ├── University.cs
│   │   ├── EntryRequest.cs
│   │   ├── StudentEnrollment.cs
│   │   ├── TeacherEnrollment.cs
│   │   └── Student.cs (legacy placeholder, removed in favor of enrollments)
│   ├── Dtos/
│   └── SideModels/
└── Core.Tests/
    ├── BaseUserServiceTest.cs
    ├── StudentServiceTests.cs
    ├── TeacherServiceTest.cs
    ├── DirectorServiceTests.cs
    ├── UniversityServiceTests.cs
    ├── GeneralUserInfoServiceTest.cs
    ├── MessageBusTests.cs
    └── GrpcServiceTest.cs
```

---

## Architecture

### Layering

```text
Core.API  ->  Core.DL  ->  Core.DAL
```

### Layer Responsibilities

- `Core.API`
  - HTTP controllers
  - gRPC server endpoint
  - RabbitMQ background subscriber
  - DI and middleware bootstrap
- `Core.DL`
  - Business rules and role transitions
  - Invite/request flow orchestration
  - Membership and permission checks
- `Core.DAL`
  - EF Core models and DbContext mapping
  - DTOs and response models
- `Core.Tests`
  - Unit tests for services, message processing, and gRPC behavior

### Persistence Pattern

All entities are accessed through generic repository abstraction:

```csharp
GetContext()
GetAllAsync()
GetByIdAsync(long id)
Where(Expression<Func<T, bool>>)
SingleOrDefaultAsync(Expression<Func<T, bool>>)
AddAsync(T)
UpdateAsync(T)
DeleteAsync(T)
DeleteRangeAsync(List<T>)
```

---

## Data Model (Current)

### Core Entities

- `BaseUser`
  - Identity mirror from Authentication (`OriginalId`, `Email`, `Username`)
  - Personal profile fields and owned `Address`
  - Optional teacher link via `TeacherId`
  - Navigation collections for `StudentEnrollments`, `TeacherEnrollments`, `EntryRequests`, and directed universities
- `Teacher`
  - One-to-one with `BaseUser`
  - `Degree`, `IsVerified`
- `University`
  - `Name`, `Description`, `IsOpened`
  - `DirectorId` (to `BaseUser`)
  - Member collections (`StudentEnrollments`, `TeacherEnrollments`)
- `EntryRequest`
  - Request/invite between user and university
  - `SentByUniversity` differentiates invite vs user request
  - `InviteAsTeacher` differentiates teacher vs student invite intent
- `StudentEnrollment`
  - Join table for student membership (`BaseUserId`, `UniversityId`)
- `TeacherEnrollment`
  - Join table for teacher membership (`BaseUserId`, `UniversityId`)

### Relationships in DbContext

- `BaseUser` (1) -> (0..1) `Teacher`
- `BaseUser` (1) -> (M) `EntryRequest`
- `University` (1) -> (M) `EntryRequest` with delete restrict
- `University` (1) -> (M) `StudentEnrollment` with unique `(BaseUserId, UniversityId)`
- `University` (1) -> (M) `TeacherEnrollment` with unique `(BaseUserId, UniversityId)`
- `University` -> `Director` (`BaseUser`) with delete restrict
- `Address` is owned by both `BaseUser` and `University`

---

## External Services and Integrations

## 1) Authentication / OpenID Connect JWT validation

Configured in `Program.cs` via `AddAuthentication().AddJwtBearer(...)`.

- Uses OpenID settings from config:
  - `OpenId:Issuer`
  - `OpenId:MetadataAddress`
  - `OpenId:PublicKey` (fallback RSA key)
- Controllers mostly require `[Authorize]` except selected anonymous actions.

## 2) RabbitMQ

- Hosted background worker: `MessageBusSubscriber`.
- Connects to `RabbitMQHost` / `RabbitMQPort`.
- Declares fanout exchange `trigger` and binds an auto-generated queue.
- For each received message, calls `IEventProcessor.ProcessEvent`.

## 3) Event Processing

`EventProcessor` currently handles:

- `BaseUser_Published`
  - Deserializes `BaseUserPublishDto`
  - Maps into `BaseUser` via AutoMapper
  - Inserts only when `OriginalId` is not already present

## 4) gRPC (for Courses and Forums)

Service: `GrpcUserInfoService` (`GrpcUsers.GrpcUsersBase`)

Methods:

- `GetInfoForTopic`
  - Input: user email + university name
  - Output: userId, universityId, and access flags
  - `isDirector` marks the university owner/director as allowed alongside enrolled members
- `GetUniversityName`
  - Input: universityId
  - Output: universityName
- `GetUserName`
  - Input: userId
  - Output: username
- `GetUserUniversityRole`
  - Input: userId + universityId
  - Output role string (`Manager`, `Teacher`, `Student`, or `Unknown`)

Proto file is exposed at:

- `GET /protos/userinfo.proto`

---

## HTTP API Functionality

Base route prefixes:

- `api/u/user`
- `api/u/student`
- `api/u/teacher`
- `api/u/director`
- `api/u/university`
- `api/u/general`

### Base User Operations

- List users
- Get user by username
- Update own profile
- View invites
- Accept/reject invite
- Leave university
  - If leaver is director, university is deleted together with enrollments and requests

### Student Operations

- Become student (`POST become`)
  - Clears teacher record and teacher enrollments
  - Clears pending requests/invites for user
- Request to join open university (`POST request/{universityName}`)
- Direct entry (`POST entry/{universityName}`)
  - Works for open universities
  - For closed universities requires matching student invite

### Teacher Operations

- Become teacher (`POST become`)
  - Clears student enrollments and pending requests
  - Creates `Teacher` record
- Verify teacher status (`POST verify` with degree)
- Teacher self-request to university is currently disabled by business rule:
  - returns message: teachers can join by invite only

### Director Operations

- Invite student (`POST invites/student`)
- Invite verified teacher (`POST invites/teacher`)
- Remove member (`DELETE members/remove`)
- Update university info (`POST update`)

### University Operations

- Paged catalog (`GET /`, anonymous)
- My universities (`GET /my`, auth)
- Available catalog not yet joined/directed by caller (`GET /catalog/available`, auth)
- Get university by name (`GET /{name}`, auth and membership required)
- Create university (`POST /`, auth)
- Paged teachers/students by university (`GET /{name}/teachers`, `GET /{name}/students`) for members only

### General Info Operation

- `GET /api/u/general/{userEmail}`
  - Returns role flags model (`isTeacher`, `isStudent`)

---

## Key Business Rules

- User may be teacher or non-teacher at a given moment (`TeacherId` controls teacher state).
- Teacher enrollment and student enrollment are separate explicit tables.
- Director is not represented in enrollment tables; director is linked by `University.DirectorId`.
- Duplicate membership is prevented by unique indexes and service checks.
- Requests and invites are cleaned up when membership is granted.
- Invite direction:
  - `SentByUniversity = true`: director invite
  - `SentByUniversity = false`: user-originated request
- Invite role intent:
  - `InviteAsTeacher = true`: teacher invite
  - `InviteAsTeacher = false`: student invite

---

## Dependency Injection and Runtime Wiring

Configured in `Program.cs`:

- Repositories (`Transient`):
  - `University`, `BaseUser`, `Teacher`, `EntryRequest`, `StudentEnrollment`, `TeacherEnrollment`
- Domain services (`Transient`):
  - `IBaseUserService`, `IUniversityService`, `IStudentService`, `ITeacherService`, `IDirectorService`, `IGeneralInfoService`
- Event processor (`Singleton`):
  - `IEventProcessor`
- Background subscriber (`HostedService`):
  - `MessageBusSubscriber`
- Database:
  - SQL Server `DataContext`
  - Migrations assembly: `Core.API`
  - Auto-migrate on startup (`Database.Migrate()`)
- Endpoints:
  - HTTP API + gRPC (`http://+:666`), plus HTTP endpoint (`http://+:8080`) from Kestrel config

---

## Configuration Summary

### Production (`Core.API/appsettings.json`)

- SQL Server: `mssql-core-clusterip-srv:1433`
- RabbitMQ: `rabbitmq-clusterip-srv:5672`
- OpenID issuer/metadata: `auth-clusterip-srv`
- Kestrel:
  - gRPC on `:666` (HTTP/2)
  - Web API on `:8080` (HTTP/1)

### Development (`Core.API/appsettings.Development.json`)

- SQL Server: `localhost:1434`
- RabbitMQ: `localhost:5672`
- OpenID issuer/metadata: `localhost:5000`

---

## Tests

Current test projects validate:

- Base user operations
- Student and teacher role flows
- Director invite/approval/removal flows
- University listing/creation/membership visibility
- General role info
- RabbitMQ event processing behavior
- gRPC lookups, director-aware access, and role resolution

This keeps Core behavior test-covered around its most critical orchestration paths.
