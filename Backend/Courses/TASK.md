# Courses Service — Analysis & Task List

## Goal

Each course has lessons. Lessons support rich content (Markdown/HTML) so teachers can create well-structured educational material. The infrastructure for this already exists — the analysis below describes what is broken, what is missing, and what needs clarification.

---

## What Already Exists

- `Course` entity with `Title`, `Description`, `TeacherId`, `UniversityId`
- `Lesson` entity with `Title`, `Content` (max 10,000 chars), `IsMarkdown`, `VideoLink`, `MaterialLink`, `OrderNumber`
- `MarkdownService` using Markdig (AdvancedExtensions + Emoji + SoftlineBreak)
- `RenderedContent` in `ReadLessonDto` — populated at read time when `IsMarkdown == true`
- Full CRUD for both courses and lessons
- Authorization via gRPC (IsAllowed / IsTeacher / ownership)

---

## Bugs / Logic Issues

### 1. `UpdateLessonDto.IsMarkdown` is present but silently ignored
**File:** `Courses.DAL/Dtos/UpdateLessonDto.cs:11` + `Courses.DL/Services/LessonService.cs:219-233`

`UpdateLessonDto` exposes `public bool? IsMarkdown { get; set; }` but `UpdateLessonAsync` never reads or applies it. The field is dead — it accepts input and throws it away with no error or feedback. The COURSES.md says `IsMarkdown` is immutable after creation, which is the right design, but the DTO should not expose the field at all, as it misleads API consumers.

**Fix:** Remove `IsMarkdown` from `UpdateLessonDto`.

---

### 2. Read endpoints return `BadRequest` for unauthorized/anonymous users
**File:** `Courses.API/Controllers/LessonController.cs:28-29`, `CourseController.cs:27-28`

The stated design is: *"Anonymous users receive empty result sets rather than 401/403."* But the actual behavior is:
- `getUserEmail()` returns `""` for unauthenticated requests (`BaseController.cs:13`)
- The gRPC call proceeds with an empty email
- If `IsAllowed == false`, the service returns `Success = false` with "You don't have such rights"
- The controller then returns `400 Bad Request`

This is incorrect on two levels: the status code is wrong (400 is for malformed requests, not access denial), and the design intent (empty results, not an error) is not implemented.

**Fix:** For read operations, when `Success == false` due to "not allowed", return `Ok` with an empty/null result rather than `BadRequest`.

---

### 3. `ReadLessonDto` is missing `CourseId`
**File:** `Courses.DAL/Dtos/ReadLessonDto.cs`

When a client fetches a single lesson by ID (`GET /api/c/lessons/lesson/{lessonId}`), the response contains no `CourseId`. The client has no way to know which course the lesson belongs to or navigate back to it.

**Fix:** Add `public long CourseId { get; set; }` to `ReadLessonDto` and populate it in both `GetLessonAsync` and `GetCourseLessonsAsync`.

---

### 4. No input length validation on DTOs
**File:** `Courses.DAL/Dtos/CreateLessonDto.cs`, `CreateCourseDto.cs`, etc.

The entity models have `[MaxLength]` constraints, but the DTOs have no validation attributes. Service methods only check for empty strings. A title of 5,000 characters would pass service validation and crash at the database layer with an unclean `DbUpdateException` rather than a proper validation error.

**Fix:** Add `[Required]` and `[MaxLength]` attributes to DTO properties matching the entity constraints.

---

### 5. `GetCourseLessonsAsync` renders full markdown for every lesson on every call
**File:** `Courses.DL/Services/LessonService.cs:53-65`

When listing all lessons of a course, the service renders the full markdown content of every lesson to HTML. For a list/overview use case, the client typically only needs titles and metadata — not the full rendered body. This wastes CPU on every list call.

**Fix:** Either return raw `Content` in list responses and only render in the single-lesson endpoint, or add a `summary` field. At minimum, document the behavior.

---

## Missing Features

### 6. No bulk lesson reorder endpoint
Teachers can update `OrderNumber` via `PUT /api/c/lessons/update`, but only one lesson at a time. Reordering 10 lessons requires 10 separate API calls. A dedicated reorder endpoint (e.g., `PUT /api/c/lessons/reorder` accepting `[{ lessonId, orderNumber }]`) would be significantly better UX for the frontend.

---

### 7. Content size limit may be too small
**File:** `Courses.DAL/Models/Lesson.cs:13`

`Content` is capped at 10,000 characters. A lesson with code examples, multiple sections, and explanatory text can easily exceed this. This is a design decision — needs confirmation whether 10,000 is intentional or a placeholder.

**Question for owner:** Should `Content` stay at 10,000, or should it be increased (e.g., 50,000 or unlimited via `nvarchar(MAX)`)?

---

### 8. No URL format validation for `VideoLink` / `MaterialLink`
**File:** `Courses.DAL/Models/Lesson.cs:18-20`, `Courses.DAL/Dtos/CreateLessonDto.cs`

These fields accept any string up to 500 chars. No validation that the value is actually a URL. A bad value would be stored and served to clients.

**Fix:** Add `[Url]` attribute to `VideoLink` and `MaterialLink` in the DTOs.

---

## Questions Before Starting

1. **Content limit** — Keep 10,000 or increase? (see item 7)
2. **Read authorization** — Should truly anonymous (no JWT) users get an empty list, or should they always get a 401? The current stated design is "empty list", but that means unauthenticated users can see university courses if the gRPC call allows it.
3. **Bulk reorder** — Should a reorder endpoint be added as part of this task?

---

## Implementation Order (once questions are answered)

1. Remove `IsMarkdown` from `UpdateLessonDto`
2. Add `CourseId` to `ReadLessonDto` and populate it in both service methods
3. Add validation attributes to all DTOs
4. Fix read endpoint response behavior (empty vs BadRequest for unauthorized)
5. Add `[Url]` validation to link fields
6. (Optional) Add bulk reorder endpoint
7. (Optional) Increase `Content` max length + new migration
