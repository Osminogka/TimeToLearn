# Courses Service

Manages university courses and their lessons. Access control is delegated entirely to the Core service via gRPC — this service has no user or university tables of its own.

---

## Project Structure

```
Courses/
├── Courses.API/
│   ├── Controllers/
│   │   ├── BaseController.cs         # JWT claim helpers, exception → HTTP mapping
│   │   ├── CourseController.cs       # CRUD courses (api/c/courses)
│   │   └── LessonController.cs       # CRUD lessons (api/c/lessons)
│   ├── Infrastructure/
│   │   └── Mapper.cs                 # AutoMapper: gRPC response types → domain models
│   ├── appsettings.json              # Production config (K8S cluster endpoints)
│   ├── appsettings.Development.json  # Local dev config
│   └── Program.cs                    # DI registration, middleware pipeline
├── Courses.DL/
│   ├── Grpc/
│   │   ├── IUserInfoClient.cs        # gRPC client interface
│   │   └── UserInfoClient.cs         # gRPC client → Core service (port 666)
│   ├── Proto/
│   │   └── userinfo.proto            # Shared proto definition (mirrors Core service)
│   ├── Repositories/
│   │   ├── IBaseRepository.cs        # Generic CRUD interface
│   │   └── BaseRepository.cs         # EF Core implementation
│   └── Services/
│       ├── ICourseService.cs / CourseService.cs
│       ├── ILessonService.cs / LessonService.cs
│       └── IMarkdownService.cs / MarkdownService.cs  # Markdig HTML renderer
├── Courses.DAL/
│   ├── Context/
│   │   └── DataContext.cs            # EF Core DbContext; Course→Lesson cascade delete
│   ├── Models/
│   │   ├── BaseEntity.cs             # Abstract base: long Id
│   │   ├── Course.cs                 # Title, Description, TeacherId, UniversityId, timestamps
│   │   └── Lesson.cs                 # Title, Content, IsMarkdown, links, OrderNumber, timestamps
│   ├── Dtos/
│   │   ├── CreateCourseDto.cs        # Title, Description, UniversityName
│   │   ├── ReadCourseDto.cs          # Includes TeacherName (resolved via gRPC), LessonsCount
│   │   ├── UpdateCourseDto.cs        # CourseId + nullable Title/Description
│   │   ├── CreateLessonDto.cs        # CourseId, Title, Content, IsMarkdown, links, OrderNumber
│   │   ├── ReadLessonDto.cs          # Includes RenderedContent (HTML, only if IsMarkdown=true)
│   │   └── UpdateLessonDto.cs        # LessonId + nullable fields (IsMarkdown is NOT updatable)
│   └── SideModels/
│       ├── ResponseMessage.cs        # Success + Message
│       ├── ResponseArray.cs          # Success + Message + IEnumerable<T>
│       ├── ResponseWithValue.cs      # Success + Message + T?
│       └── UserInfoForCourse.cs      # gRPC result: UserId, UniversityId, IsAllowed, IsTeacher
└── Courses.Tests/
    └── UnitTest1.cs                  # Empty placeholder — no tests implemented
```

---

## Architecture

### Layer Responsibilities

| Layer | Responsibility |
|-------|---------------|
| **API** | HTTP routing, JWT validation, startup wiring |
| **DL** | Business logic, gRPC calls, markdown rendering, repository orchestration |
| **DAL** | EF Core entities, DbContext, DTOs, response wrappers |

### Dependencies Between Layers

```
Courses.API
    └── Courses.DL
            └── Courses.DAL
```

### Generic Repository

All persistence goes through `IBaseRepository<T>` / `BaseRepository<T>`:

```csharp
AddAsync(T)
UpdateAsync(T)             // fetches by ID first, then SetValues — avoids detached entity issues
DeleteAsync(T)
DeleteRangeAsync(List<T>)
GetAllAsync()
GetByIdAsync(long id)
Where(Expression<Func<T, bool>>)          // deferred — used for pagination and includes
SingleOrDefaultAsync(Expression<Func<T, bool>>)
```

Two repositories registered: `IBaseRepository<Course>` and `IBaseRepository<Lesson>`, both `Transient`.

---

## Data Model

```
Course
  Id, Title (max 100), Description (max 2000)
  TeacherId (long)      ← user ID from Core service, not an FK
  UniversityId (long)   ← university ID from Core service, not an FK
  CreatedAt, UpdatedAt?
  └── Lessons (1:N, cascade delete)

Lesson
  Id, Title (max 200), Content (max 10 000)
  IsMarkdown (bool, default true, immutable after creation)
  VideoLink?, MaterialLink? (max 500 each)
  OrderNumber (int)     ← controls display sequence
  CourseId (FK)
  CreatedAt, UpdatedAt?
```

`TeacherId` and `UniversityId` on `Course` are plain `long` columns — there are no local user or university tables. Identity is resolved on demand via gRPC.

---

## Endpoints

### `CourseController` — `api/c/courses`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/{universityName}/{page}` | Optional | Paginated courses for a university (10/page) |
| GET | `/course/{courseId}` | Optional | Single course with lesson count |
| POST | `/create` | Required | Create course (teacher only) |
| PUT | `/update` | Required | Update title/description (owner teacher only) |
| DELETE | `/delete/{courseId}` | Required | Delete course + all lessons (owner teacher only) |

### `LessonController` — `api/c/lessons`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/course/{courseId}` | Optional | All lessons for a course, ordered by `OrderNumber` |
| GET | `/lesson/{lessonId}` | Optional | Single lesson; markdown rendered to HTML if `IsMarkdown=true` |
| POST | `/create` | Required | Create lesson (owner teacher only) |
| PUT | `/update` | Required | Partial update (owner teacher only); `IsMarkdown` cannot be changed |
| DELETE | `/delete/{lessonId}` | Required | Delete lesson (owner teacher only) |

---

## Authorization Model

All authorization is resolved by calling the Core service via gRPC — this service never stores roles.

| Operation | Check |
|-----------|-------|
| Read (get courses/lessons) | `UserInfoForCourse.IsAllowed == true` (user is university member) |
| Create | `IsAllowed && IsTeacher` |
| Update / Delete | `IsAllowed && IsTeacher && course.TeacherId == reply.UserId` |

Anonymous users receive empty result sets for read operations rather than 401/403 errors.

---

## gRPC Integration

**Client:** `UserInfoClient` (Scoped) calls the Core service at `GrpcUsersApi` config endpoint.

| gRPC Method | When Called | Returns |
|-------------|------------|---------|
| `GetInfoForTopic(universityName, email)` | Every write + every read | `UserInfoForCourse { UserId, UniversityId, IsAllowed }` |
| `GetUniversityName(universityId)` | Reads that start from a courseId | University name string |
| `GetUserName(userId)` | Populating `TeacherName` in `ReadCourseDto` | Username string |

gRPC failures are caught, logged to console, and return `null` — callers treat null as "not allowed."

---

## Markdown Support

- `MarkdownService` (Transient) wraps **Markdig** with `AdvancedExtensions`, `EmojiAndSmiley`, and `SoftlineBreakAsHardlineBreak`
- Rendering happens at read time in the service layer — HTML is never persisted
- `ReadLessonDto.RenderedContent` is populated only when `Lesson.IsMarkdown == true`
- `IsMarkdown` is set at creation and cannot be changed via update

---

## Configuration

### Production (`appsettings.json`)
```
DB:          Server=mssql-courses-clusterip-srv,1433; Initial Catalog=Data
gRPC:        http://users-clusterip-srv:666
JWT Key:     shared with all services
RabbitMQ:    rabbitmq-clusterip-srv:5672  (configured but unused)
```

### Development (`appsettings.Development.json`)
```
DB:          Server=localhost,1438; Initial Catalog=DataTest
gRPC:        http://localhost:666
RabbitMQ:    localhost:5672
```

---

## DI Lifetimes

| Service | Lifetime |
|---------|----------|
| `IBaseRepository<Course>` | Transient |
| `IBaseRepository<Lesson>` | Transient |
| `ICourseService` | Transient |
| `ILessonService` | Transient |
| `IMarkdownService` | Transient |
| `IUserInfoClient` | **Scoped** |

---

## Key Implementation Notes

- **No RabbitMQ usage** — connection strings are in config but nothing is published or consumed
- **No tests implemented** — `Courses.Tests` contains only an empty placeholder class
- **Pagination** — courses only; lessons are always returned in full ordered by `OrderNumber`
- **Partial updates** — all `Update*Dto` fields are nullable; service only applies non-null values
- **`UpdatedAt`** is set in the service layer on every update, not via EF interceptors
