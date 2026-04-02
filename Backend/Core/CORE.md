# Core Service

Manages users, universities, and roles (student/teacher/director). Receives new user events from RabbitMQ and exposes a gRPC server used by Courses and Forums services to resolve user and university identity.

---

## Project Structure

```
Core/
├── Core.API/
│   ├── Controllers/
│   │   ├── BaseController.cs             # JWT claim helpers, exception → HTTP mapping
│   │   ├── BaseUserController.cs         # User info, invites
│   │   ├── StudentController.cs          # Become student, request/enter university
│   │   ├── TeacherController.cs          # Become teacher, verify degree, request university
│   │   ├── DirectorController.cs         # Invite/approve/reject university members
│   │   ├── UniversityController.cs       # CRUD universities, list members
│   │   └── GeneralinfoController.cs      # Role info (public, no auth)
│   ├── AsyncDataService/
│   │   └── MessageBusSubscriber.cs       # RabbitMQ background subscriber
│   ├── EventProcessing/
│   │   ├── IEventProcessor.cs
│   │   └── EventProcessor.cs             # Deserializes events, creates BaseUser records
│   ├── Grpc/
│   │   └── GrpcUserinfoService.cs        # gRPC server (port 666)
│   ├── Infrastructure/
│   │   └── Mapper.cs                     # AutoMapper profiles
│   ├── appsettings.json                  # Production config (K8S cluster endpoints)
│   ├── appsettings.Development.json      # Local dev config
│   └── Program.cs                        # DI registration, middleware pipeline
├── Core.DL/
│   ├── Services/
│   │   ├── BaseUserService.cs            # User info, invites, accept/reject
│   │   ├── UniversityService.cs          # University CRUD, member lists
│   │   ├── StudentService.cs             # Role transitions, entry requests
│   │   ├── TeacherService.cs             # Role transitions, degree verification
│   │   ├── DirectorService.cs            # Invite, approve, reject, update university
│   │   └── GeneralUserInfoService.cs     # isTeacher / isStudent flags
│   └── Repositories/
│       ├── IBaseRepository.cs            # Generic interface
│       └── BaseRepository.cs             # Generic EF Core implementation
├── Core.DAL/
│   ├── Context/
│   │   └── DataContext.cs                # EF Core DbContext, relationships
│   ├── Models/
│   │   ├── BaseEntity.cs                 # Abstract base: long Id
│   │   ├── Address.cs                    # Owned entity (embedded in users & universities)
│   │   ├── BaseUser.cs                   # Core user identity
│   │   ├── Student.cs                    # One-to-one with BaseUser
│   │   ├── Teacher.cs                    # One-to-one with BaseUser (has Degree, IsVerified)
│   │   ├── University.cs                 # University entity
│   │   └── EntryRequest.cs               # Pending invites / join requests
│   ├── Dtos/
│   │   ├── BaseUserPublishDto.cs          # Incoming RabbitMQ payload
│   │   ├── GenericEventDto.cs             # Used to determine event type before full deserialization
│   │   ├── ReadBaseUserDto.cs
│   │   ├── CreateUniversityDto.cs
│   │   └── ReadUniversityDto.cs
│   └── SideModels/
│       ├── ResponseMessage.cs             # Success + Message
│       ├── ResponseWithValue.cs           # Success + Message + Value<T>
│       ├── ResponseGetEnum.cs             # Success + Message + IEnumerable<T>
│       ├── EntryRequestModel.cs           # UniversityName + Username
│       ├── UpdateUserInfoModel.cs
│       ├── UpdateUniversityInfoModel.cs
│       └── RoleUserInfo.cs                # isTeacher, isStudent flags
└── Core.Tests/
    ├── BaseUserServiceTest.cs
    ├── UniversityServiceTests.cs
    ├── StudentServiceTests.cs
    ├── TeacherServiceTest.cs
    ├── DirectorServiceTests.cs
    ├── GeneralUserInfoServiceTest.cs
    ├── MessageBusTests.cs
    └── GrpcServiceTest.cs
```

---

## Architecture

### Layer Responsibilities

| Layer | Responsibility |
|-------|---------------|
| **API** | HTTP routing, gRPC server, RabbitMQ subscription, startup wiring |
| **DL** | Business logic, validation, role transitions, repository orchestration |
| **DAL** | EF Core entities, DbContext, DTOs, request/response models |
| **Tests** | Unit tests against real in-memory database; mocks only for gRPC context |

### Dependencies Between Layers

```
Core.API
    └── Core.DL
            └── Core.DAL
```

### Generic Repository

All persistence goes through a single generic `IBaseRepository<T>` / `BaseRepository<T>`:

```csharp
GetAllAsync()
GetByIdAsync(long id)
Where(Expression<Func<T, bool>>)
SingleOrDefaultAsync(Expression<Func<T, bool>>)
AddAsync(T)
UpdateAsync(T)       // uses Entry().CurrentValues.SetValues()
DeleteAsync(T)
DeleteRangeAsync(List<T>)
GetContext()         // exposes DbContext for .Include() chains in services
```

Each entity type gets its own `IBaseRepository<T>` registration — five repositories total (`University`, `BaseUser`, `Student`, `Teacher`, `EntryRequest`), all `Transient`.

---

## Data Model

### Entities & Relationships

```
BaseUser (1) ──────────────── (0..1) Student
BaseUser (1) ──────────────── (0..1) Teacher  [has Degree, IsVerified]
BaseUser (1) ──────────────── (0..1) University  [as director]
BaseUser (M) ──────────────── (N) University    [members, implicit join table]
BaseUser (1) ──────────────── (M) EntryRequest
University (1) ──────────────── (M) EntryRequest  [cascade restricted]
```

### Key Fields

**`BaseUser`**
- `OriginalId` (Guid) — links to the Authentication service user
- `IsTeacher` (bool) — denormalized flag for quick gRPC checks
- `TeacherId?` / `StudentId?` — nullable FKs; only one is set at a time

**`Teacher`**
- `Degree` (string), `IsVerified` (bool) — teacher must be verified before joining universities

**`EntryRequest`**
- `SentByUniversity` (bool) — `true` = director invited user; `false` = user requested to join

**`Address`** — owned entity embedded in both `BaseUser` and `University` (not a separate table)

---

## Endpoints

### `BaseUserController` — `api/u/user`

| Method | Path | Description |
|--------|------|-------------|
| GET | `/all` | List all usernames |
| GET | `/{name}` | Get user details by username |
| POST | `/update` | Update own profile (name, phone, address) |
| GET | `/invites` | List pending university invitations (SentByUniversity=true) |
| PUT | `/invites/{universityName}/accept` | Accept invitation → joins university |
| DELETE | `/invites/{universityName}/reject` | Reject invitation |

### `StudentController` — `api/u/student`

| Method | Path | Description |
|--------|------|-------------|
| GET | `/become` | Become a student (clears teacher role) |
| GET | `/request/{universityName}` | Send join request to university (must be open) |
| GET | `/entry/{universityName}` | Enter university directly (if open) or via invite |

### `TeacherController` — `api/u/teacher`

| Method | Path | Description |
|--------|------|-------------|
| GET | `/become` | Become a teacher (clears student role) |
| POST | `/verify` | Submit degree → sets IsVerified=true |
| POST | `/request/{universityName}` | Request to join university (verified teachers only) |

### `DirectorController` — `api/u/director`

| Method | Path | Description |
|--------|------|-------------|
| POST | `/invites/student` | Invite student to own university |
| POST | `/invites/teacher` | Invite verified teacher to own university |
| POST | `/invites/approve` | Approve a user's join request |
| POST | `/invites/reject` | Reject a user's join request |
| POST | `/update` | Update university description/address |

### `UniversityController` — `api/u/university`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/` | No | List all universities |
| GET | `/{name}` | No | Get university details |
| POST | `/` | Yes | Create university (caller becomes director) |
| GET | `/{name}/teachers` | Yes (member only) | List teacher members |
| GET | `/{name}/students` | Yes (member only) | List student members |

### `GeneralinfoController` — `api/u/general`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/{userEmail}` | No | Returns `{ isTeacher, isStudent }` flags |

---

## Business Rules

### Role Transitions
- A user starts with no role (BaseUser only)
- Calling `/become` on either student or teacher **clears the other** (`TeacherId` or `StudentId` set to null)
- Teachers must call `/teacher/verify` (submit a degree) before they can request to join a university

### University Entry — Four Paths

| Scenario | Who initiates | How it resolves |
|----------|--------------|-----------------|
| Open university, user requests | Student/Teacher | `/student/request` or `/teacher/request` → Director approves via `/director/invites/approve` |
| Open university, user enters directly | Student | `/student/entry` succeeds without approval |
| Closed university, director invites | Director | `/director/invites/student` or `/teacher` → User accepts via `/user/invites/{name}/accept` |
| User has existing invite | Student | `/student/entry` also works if invite exists |

After a user joins, all related `EntryRequest` records for that user/university pair are cleaned up.

---

## RabbitMQ Integration

- **`MessageBusSubscriber`** — `BackgroundService`; connects on startup, declares fanout exchange `"trigger"`, listens for messages
- **`EventProcessor`** (Singleton) — reads `GenericEventDto.Event` to route the message:
  - `"BaseUser_Published"` → deserializes as `BaseUserPublishDto`, maps to `BaseUser`, inserts if `OriginalId` not already present
- A new DI scope is created per message to resolve the `Transient` `IBaseRepository<BaseUser>`

---

## gRPC Server

**Port:** 666  
**Service:** `GrpcUserinfoService` implements `GrpcUsers.GrpcUsersBase`

| Method | Request | Response | Usage |
|--------|---------|----------|-------|
| `GetInfoForTopic` | `{ userEmail, universityName }` | `{ userId, universityId, hasPermission }` | Courses/Forums check membership |
| `GetUniversityName` | `{ universityId }` | `{ universityName }` | Resolve university name |
| `GetUserName` | `{ userId }` | `{ userName }` | Resolve username |

Proto file served at: `GET /protos/userinfo.proto`

---

## Configuration

### Production (`appsettings.json`)
```
DB:       Server=mssql-users-clusterip-srv,1433; Initial Catalog=Data
RabbitMQ: rabbitmq-clusterip-srv:5672
gRPC:     http://users-clusterip-srv:666
HTTP:     http://users-clusterip-srv:80
JWT Key:  shared with all services
```

### Development (`appsettings.Development.json`)
```
DB:       Server=localhost,1434; Initial Catalog=Data
RabbitMQ: localhost:5672
```

---

## DI Lifetimes

| Service | Lifetime | Reason |
|---------|----------|--------|
| `IBaseRepository<T>` (×5) | Transient | Scoped to request, holds EF DbContext |
| All domain services | Transient | Per-request |
| `EventProcessor` | **Singleton** | Uses `IServiceScopeFactory` to create its own scopes per message |
| `MessageBusSubscriber` | **HostedService** | Long-running background listener |

---

## Tests

All tests use EF Core **in-memory database** — no mocks for repositories or services.  
Mocks used only for `ServerCallContext` (gRPC) and `IServiceScopeFactory` (message bus).

| Test Class | Covers |
|------------|--------|
| `BaseUserServiceTest` | GetUsers, GetUser, UpdateInfo, GetInvites, AcceptInvite, RejectInvite |
| `UniversityServiceTests` | CreateUniversity, GetAll, GetUniversity, GetTeachers, GetStudents |
| `StudentServiceTests` | BecomeStudent, SendRequest, EntryUniversity |
| `TeacherServiceTest` | BecomeTeacher, VerifyStatus, SendRequest |
| `DirectorServiceTests` | InviteStudent, InviteTeacher, AcceptRequest, RejectRequest, UpdateUniversity |
| `GeneralUserInfoServiceTest` | GetRole — teacher-only, student-only, neither |
| `MessageBusTests` | Event deserialization → BaseUser creation, duplicate prevention |
| `GrpcServiceTest` | GetInfoForTopic, GetUniversityName, GetUserName |
