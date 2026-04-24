# Courses Service

Manages university courses, lessons, progress tracking, and quiz flows. Access control is delegated entirely to the Core service via gRPC — this service has no user or university tables of its own.

---

## Project Structure

```
Courses/
├── Courses.API/
│   ├── Controllers/
│   │   ├── BaseController.cs         # JWT claim helpers, exception → HTTP mapping
│   │   ├── CourseController.cs       # CRUD courses (api/c/courses)
│   │   ├── LessonController.cs       # CRUD lessons (api/c/lessons)
│   │   └── QuizController.cs         # Lesson mini quizzes + course quizzes (api/c/quizzes)
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
│       ├── IMarkdownService.cs / MarkdownService.cs  # Markdig HTML renderer
│       ├── IProgressService.cs / ProgressService.cs  # Lesson completion + course progress + grading
│       └── IQuizService.cs / QuizService.cs          # Quiz question lifecycle + answer checks
├── Courses.DAL/
│   ├── Context/
│   │   └── DataContext.cs            # EF Core DbContext; Course→Lesson cascade delete
│   ├── Models/
│   │   ├── BaseEntity.cs             # Abstract base: long Id
│   │   ├── Course.cs                 # Title, Description, TeacherId, UniversityId, timestamps
│   │   ├── Lesson.cs                 # Title, Content, IsMarkdown, links, OrderNumber, timestamps
│   │   ├── LessonResource.cs         # LessonId, Title, Url, Type, timestamps
│   │   ├── StudentLessonCompletion.cs
│   │   ├── StudentCourseGrade.cs
│   │   ├── QuizQuestion.cs           # Question for lesson mini quiz or course quiz mode
│   │   ├── QuizOption.cs             # Single-choice answer option (one correct)
│   │   └── QuizAnswer.cs             # Student selected option + correctness snapshot
│   ├── Dtos/
│   │   ├── CreateCourseDto.cs        # Title, Description, UniversityName
│   │   ├── ReadCourseDto.cs          # Includes TeacherName (resolved via gRPC), LessonsCount
│   │   ├── UpdateCourseDto.cs        # CourseId + nullable Title/Description
│   │   ├── LessonResourceDto.cs      # Resource item: Title, Url, Type
│   │   ├── CreateLessonDto.cs        # CourseId, Title, Content, IsMarkdown, legacy links, resources[], OrderNumber
│   │   ├── ReadLessonDto.cs          # Includes CourseId, RenderedContent, resources[], completion metadata
│   │   ├── UpdateLessonDto.cs        # LessonId + nullable fields + optional resources[] replacement
│   │   ├── CreateQuizQuestionDto.cs / CreateQuizOptionDto.cs
│   │   ├── QuizQuestionDto.cs / QuizOptionDto.cs
│   │   ├── UpsertQuizDto.cs          # Bulk upsert quiz + attempt policy (single/reattempt)
│   │   ├── SubmitQuizDto.cs / SubmitQuizQuestionAnswerDto.cs
│   │   ├── ReorderLessonsDto.cs      # Bulk lesson ordering
│   │   ├── StudentQuizAnswerDto.cs
│   │   └── QuizAnswerReviewDto.cs
│   └── SideModels/
│       ├── ResponseMessage.cs        # Success + Message
│       ├── ResponseArray.cs          # Success + Message + IEnumerable<T>
│       ├── ResponseWithValue.cs      # Success + Message + T?
│       └── UserInfoForCourse.cs      # gRPC result: UserId, UniversityId, IsAllowed, IsTeacher, IsDirector
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

Repositories are registered for all persisted aggregates (`Course`, `Lesson`, `LessonResource`, `StudentLessonCompletion`, `StudentCourseGrade`, `QuizQuestion`, `QuizOption`, `QuizAnswer`) with `Transient` lifetime.

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
  └── Resources (1:N, cascade delete)

LessonResource
  Id, LessonId (FK)
  Title (max 120), Url (max 500), Type (max 32)
  CreatedAt, UpdatedAt?
```

`TeacherId` and `UniversityId` on `Course` are plain `long` columns — there are no local user or university tables. Identity is resolved on demand via gRPC.

---

## Endpoints

### `CourseController` — `api/c/courses`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/{universityName}/{page}` | Required | Paginated courses for a university (10/page) |
| GET | `/course/{courseId}` | Required | Single course with lesson count |
| POST | `/create` | Required | Create course (teacher only) |
| PUT | `/update` | Required | Update title/description (owner teacher only) |
| DELETE | `/delete/{courseId}` | Required | Delete course + all lessons (owner teacher only) |
| GET | `/{courseId}/progress` | Required | Current user progress for this course |
| GET | `/{courseId}/student-progress` | Required | Teacher view of tracked students progress + marks |
| POST | `/{courseId}/grade-student` | Required | Teacher assigns or updates student mark (1-10) |

### `LessonController` — `api/c/lessons`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/course/{courseId}` | Required | All lessons for a course, ordered by `OrderNumber` |
| GET | `/lesson/{lessonId}` | Required | Single lesson; markdown rendered to HTML if `IsMarkdown=true` |
| POST | `/create` | Required | Create lesson (owner teacher only) |
| PUT | `/update` | Required | Partial update (owner teacher only); `IsMarkdown` cannot be changed |
| DELETE | `/delete/{lessonId}` | Required | Delete lesson (owner teacher only) |
| PUT | `/reorder` | Required | Bulk reorder lessons in course |
| POST | `/{lessonId}/complete` | Required | Student marks lesson as completed |
| GET | `/{lessonId}/progress` | Required | Current user completion status for lesson |

### `QuizController` — `api/c/quizzes`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/lesson/{lessonId}` | Required | Get lesson mini quiz questions (students do not receive correct-answer flags) |
| PUT | `/lesson/{lessonId}` | Required | Bulk upsert lesson quiz questions + attempt policy |
| POST | `/lesson/{lessonId}/questions` | Required | Teacher/director creates lesson mini quiz question |
| PUT | `/lesson/{lessonId}/questions/{questionId}` | Required | Teacher/director updates lesson mini quiz question |
| POST | `/lesson/{lessonId}/submit` | Required | Student submits lesson quiz answers (batch payload) |
| GET | `/lesson/{lessonId}/my-answers` | Required | Student retrieves own lesson mini quiz answers + correctness |
| GET | `/lesson/{lessonId}/answers` | Required | Teacher/director reviews all student answers for lesson mini quiz |
| GET | `/course/{courseId}` | Required | Get course-level quiz questions (quiz mode without lesson content) |
| PUT | `/course/{courseId}` | Required | Bulk upsert course quiz questions + attempt policy |
| POST | `/course/{courseId}/questions` | Required | Teacher/director creates course-level quiz question |
| PUT | `/course/{courseId}/questions/{questionId}` | Required | Teacher/director updates course-level quiz question |
| POST | `/course/{courseId}/submit` | Required | Student submits course quiz answers (batch payload) |
| GET | `/course/{courseId}/my-answers` | Required | Student retrieves own course quiz answers + correctness |
| GET | `/course/{courseId}/answers` | Required | Teacher/director reviews all student answers for course-level quiz |

---

## Authorization Model

All authorization is resolved by calling the Core service via gRPC — this service never stores roles.

| Operation | Check |
|-----------|-------|
| Read (get courses/lessons) | `UserInfoForCourse.IsAllowed == true` (user is university member) |
| Create / Update / Delete | `IsAllowed && (IsTeacher || IsDirector)` |

All controllers are decorated with `[Authorize]`, so requests require a valid JWT before service-level gRPC authorization checks are executed.

---

## gRPC Integration

**Client:** `UserInfoClient` (Scoped) calls the Core service at `GrpcUsersApi` config endpoint.

| gRPC Method | When Called | Returns |
|-------------|------------|---------|
| `GetInfoForTopic(universityName, email)` | Every write + every read | `UserInfoForCourse { UserId, UniversityId, IsAllowed, IsTeacher, IsDirector }` |
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
| `GrpcChannel` | Singleton |
| `GrpcUsers.GrpcUsersClient` | Singleton |
| `IBaseRepository<LessonResource>` | Transient |
| `IBaseRepository<StudentLessonCompletion>` | Transient |
| `IBaseRepository<StudentCourseGrade>` | Transient |
| `IBaseRepository<QuizQuestion>` | Transient |
| `IBaseRepository<QuizOption>` | Transient |
| `IBaseRepository<QuizAnswer>` | Transient |
| `IBaseRepository<Course>` | Transient |
| `IBaseRepository<Lesson>` | Transient |
| `ICourseService` | Transient |
| `ILessonService` | Transient |
| `IProgressService` | Transient |
| `IQuizService` | Transient |
| `IMarkdownService` | Transient |
| `IUserInfoClient` | **Scoped** |

---

## Key Implementation Notes

- **No RabbitMQ usage** — connection strings are in config but nothing is published or consumed
- **No tests implemented** — `Courses.Tests` contains only an empty placeholder class
- **Pagination** — courses only; lessons are always returned in full ordered by `OrderNumber`
- **Partial updates** — all `Update*Dto` fields are nullable; service only applies non-null values
- **`UpdatedAt`** is set in the service layer on every update, not via EF interceptors
- **Lesson resources support** — lessons now support a full `resources[]` collection while keeping `VideoLink` / `MaterialLink` for backward compatibility
- **Quiz submission shape** — students submit batch payloads (`SubmitQuizDto`) for lesson/course quiz flows
- **Quiz authoring modes** — supports both granular create/update endpoints and bulk upsert endpoints

---

## Progress & Grading

### New Tables

`StudentLessonCompletion`
- `Id`, `StudentId`, `LessonId`, `CompletedAt`, `CreatedAt`
- Unique index on `(StudentId, LessonId)`
- Cascade delete when lesson is deleted

`StudentCourseGrade`
- `Id`, `StudentId`, `CourseId`, `Mark (1-10)`, `GivenByTeacherId`, `GivenAt`, `CreatedAt`
- Unique index on `(StudentId, CourseId)`
- Cascade delete when course is deleted

### Rules

- Only **students** can complete lessons (`/{lessonId}/complete`)
- Teachers/directors can view tracked student progress for their course and assign marks
- Mark assignment is allowed only when student has completed all lessons in the course
- Course and lesson read DTOs now include current user completion fields to support UI progress bars and completion states

---

## Quiz Mechanics

### Supported Modes

1. **Lesson mini quiz**
- Teacher attaches optional single-choice questions to a specific lesson.
- Student answers are stored per question and can be updated.

2. **Course quiz mode**
- Teacher creates quiz questions directly under course (no lesson content required).
- Useful for quiz-only courses or assessment-first flows.

### Validation Rules

- A quiz question must have at least two non-empty options.
- Exactly one option must be marked correct.
- Only students can submit quiz answers.
- Teachers/directors can create questions and review all submissions.
- Attempt policy supports `single` or `reattempt` and is configured via `UpsertQuizDto.AttemptPolicy`.

### Teacher Review Surface

- Teacher review APIs return per-answer rows containing:
  - Student identity (`StudentId`, resolved `StudentName`)
  - Question text
  - Selected option text
  - Correct option text
  - Correct/incorrect status
  - Answer timestamp

### Student Visibility

- Students can only fetch **their own** answer history and correctness status.
- Students cannot access other students answers through quiz endpoints.
