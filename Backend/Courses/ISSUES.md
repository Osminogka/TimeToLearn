# Courses Service — Issue Report

> Generated: 2026-04-03

---

## Summary

| Severity | Count |
|----------|-------|
| Critical | 2 |
| High     | 3 |
| Medium   | 5 |
| Low      | 3 |

---

## Critical

### C-1 — NullReferenceException for anonymous users calling `getUserEmail()`

**File:** `Courses.API/Controllers/BaseController.cs:14`

```csharp
// Current — throws NullReferenceException when user is not authenticated
return HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name).Value;
```

`FirstOrDefault` returns `null` when no matching claim exists (unauthenticated request). Calling `.Value` on `null` throws a `NullReferenceException`. All four `[AllowAnonymous]` endpoints (`GetUniversityCoursesAsync`, `GetCourseAsync`, `GetCourseLessonsAsync`, `GetLessonAsync`) pass the result of `getUserEmail()` directly to service methods — meaning every anonymous request to a read endpoint will crash with a 500.

**Fix:**
```csharp
protected string getUserEmail()
{
    return HttpContext.User.Claims
        .FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? string.Empty;
}
```

---

### C-2 — `IsMarkdown` flag is never assigned when creating a lesson

**File:** `Courses.DL/Services/LessonService.cs:165-174`

```csharp
Lesson lesson = new Lesson
{
    Title = lessonDto.Title,
    Content = lessonDto.Content,
    VideoLink = lessonDto.VideoLink,
    MaterialLink = lessonDto.MaterialLink,
    OrderNumber = lessonDto.OrderNumber,
    CourseId = lessonDto.CourseId,
    CreatedAt = DateTime.UtcNow
    // IsMarkdown is missing!
};
```

`CreateLessonDto.IsMarkdown` is accepted from the client but never written to the entity. The `Lesson.IsMarkdown` property defaults to `true`, so passing `IsMarkdown = false` is silently ignored — every lesson is treated as markdown regardless of what the caller requested.

**Fix:** Add `IsMarkdown = lessonDto.IsMarkdown` to the object initializer.

---

## High

### H-1 — gRPC channel created per HTTP request

**File:** `Courses.DL/Grpc/UserInfoClient.cs:20-21`

```csharp
_channel = GrpcChannel.ForAddress(_configuration["GrpcUsersApi"]);
_client = new GrpcUsers.GrpcUsersClient(_channel);
```

`UserInfoClient` is registered as `Scoped`, so a new `GrpcChannel` is created for every incoming HTTP request. `GrpcChannel` manages a connection pool internally and is designed to be a long-lived, shared singleton. Creating one per request leaks sockets, adds significant latency (TLS handshake on every request), and can exhaust ephemeral ports under load.

**Fix:** Use the built-in gRPC client factory which manages channel lifetime correctly:

```csharp
// Program.cs
builder.Services.AddGrpcClient<GrpcUsers.GrpcUsersClient>(o =>
{
    o.Address = new Uri(builder.Configuration["GrpcUsersApi"]);
});
builder.Services.AddScoped<IUserInfoClient, UserInfoClient>();

// UserInfoClient.cs — inject the generated client directly
public UserInfoClient(GrpcUsers.GrpcUsersClient client, IMapper mapper) { ... }
```

---

### H-2 — N+1 gRPC calls for teacher names on course listing

**File:** `Courses.DL/Services/CourseService.cs:46`

```csharp
foreach (var course in courses)
{
    var teacherName = await _grpcClient.GetUserName(course.TeacherId); // called N times
    ...
}
```

For a page of 10 courses, 10 sequential gRPC calls are made to look up teacher names, each going over the network. This directly multiplies latency by the page size.

**Fix (short-term):** Deduplicate by teacher ID first, then call in parallel:

```csharp
var uniqueTeacherIds = courses.Select(c => c.TeacherId).Distinct();
var nameMap = await Task.WhenAll(
    uniqueTeacherIds.Select(async id => (id, name: await _grpcClient.GetUserName(id))));
var teacherNames = nameMap.ToDictionary(x => x.id, x => x.name ?? "Unknown");
```

**Fix (long-term):** Add a `GetUserNames(IEnumerable<long> ids)` batch method to the gRPC proto and `IUserInfoClient`.

---

### H-3 — `UseDefaultFiles()` placed after `MapControllers()` — dead middleware

**File:** `Courses.API/Program.cs:76-79`

```csharp
app.MapControllers();   // pipeline ends here for all routed requests
app.UseDefaultFiles();  // never reached
```

Middleware registered after terminal endpoints is never executed. `UseDefaultFiles` must be placed before `MapControllers` (and before `UseStaticFiles`) to have any effect. For a pure API service with no static files this middleware also has no purpose and should be removed.

**Fix:** Remove `app.UseDefaultFiles()` entirely (no static file serving is configured for this service), or move it before `app.UseRouting()` if static files are intentionally needed.

---

## Medium

### M-1 — `UpdateLessonDto` exposes `IsMarkdown` but it is silently ignored

**File:** `Courses.DAL/Dtos/UpdateLessonDto.cs:11`

```csharp
public bool? IsMarkdown { get; set; }  // accepted but never read in LessonService.UpdateLessonAsync
```

The DTO accepts `IsMarkdown` from clients, strongly implying it can be changed. The service ignores it without any error or warning. This is a contract lie — clients will waste time debugging why their `IsMarkdown` changes have no effect.

**Fix:** Remove the `IsMarkdown` property from `UpdateLessonDto`. The DTO should not expose fields it doesn't support.

---

### M-2 — All business-logic failures return `400 Bad Request`

**Files:** `Courses.API/Controllers/CourseController.cs`, `LessonController.cs`

```csharp
if (!result.Success)
    return BadRequest(result.Message);
```

`400 Bad Request` means the client sent a malformed request. However the same status code is returned for:
- Not found (`"Such course doesn't exist"`) → should be `404 Not Found`
- Unauthorized (`"You don't have such rights"`) → should be `403 Forbidden`
- Business rule violations → could be `400` or `422 Unprocessable Entity`

This breaks REST conventions, makes client-side error handling harder, and leaks internal logic to the caller indiscriminately.

**Fix:** Introduce a typed result with an error category (`NotFound`, `Forbidden`, `Validation`) and map it to the correct HTTP status in the controller.

---

### M-3 — No input validation on DTOs

**Files:** `Courses.DAL/Dtos/CreateCourseDto.cs`, `CreateLessonDto.cs`, `UpdateCourseDto.cs`, `UpdateLessonDto.cs`

None of the DTO classes have `[Required]`, `[MaxLength]`, or `[MinLength]` data annotation attributes. A request body with a 50,000-character title will pass model binding and reach the database layer, where it fails with a `DbUpdateException` returned as a generic `500 Internal Server Error`. Manual checks like `IsNullOrWhiteSpace` only catch the empty-string case.

**Fix:** Mirror the model constraints on the DTOs:

```csharp
public class CreateCourseDto
{
    [Required, MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string UniversityName { get; set; } = string.Empty;
}
```

This returns `400` with a structured `ModelState` error before the service layer is ever called.

---

### M-4 — Duplicate `OrderNumber` allowed within a course

**File:** `Courses.DAL/Models/Lesson.cs:25`, `Courses.DAL/Context/DataContext.cs`

There is no unique index on `(CourseId, OrderNumber)`. Multiple lessons in the same course can share the same `OrderNumber`, producing a non-deterministic ordering. The query in `GetCourseLessonsAsync` uses `OrderBy(obj => obj.OrderNumber)` — ties are broken arbitrarily by the database engine.

**Fix:** Add a unique composite index in `DataContext.OnModelCreating`:

```csharp
modelBuilder.Entity<Lesson>()
    .HasIndex(e => new { e.CourseId, e.OrderNumber })
    .IsUnique();
```

And validate for conflicts before inserting/updating in `LessonService`.

---

### M-5 — No tests

**File:** `Courses.Tests/UnitTest1.cs`

The test project exists but contains only an empty placeholder class. The service layer contains non-trivial authorization logic (ownership checks, gRPC-based role resolution) that has no test coverage. Any refactor or fix to the service layer is unverifiable.

**Recommended coverage areas:**
- `CourseService`: all four operations — correct authorization, not-found, ownership mismatch
- `LessonService`: same, plus markdown rendering toggle
- `UserInfoClient`: gRPC failure handling (null return path)

---

## Low

### L-1 — `VideoLink` and `MaterialLink` accept any string — no URL validation

**File:** `Courses.DAL/Models/Lesson.cs:18-21`

The fields are constrained to 500 characters but accept arbitrary strings. A lesson could be saved with `VideoLink = "not a url"`. The frontend may attempt to render these as hyperlinks or embed them in `<iframe>`, which would silently fail.

**Fix:** Add a `[Url]` annotation to the DTO fields (not the model — model annotations are DB-level, URL format validation belongs at the API boundary):

```csharp
// CreateLessonDto / UpdateLessonDto
[Url, MaxLength(500)]
public string? VideoLink { get; set; }
```

---

### L-2 — `GetCourseAsync` / `GetLessonAsync` make extra gRPC round-trips for anonymous access

**Files:** `Courses.DL/Services/CourseService.cs:82-89`, `LessonService.cs:38-46`

For endpoints that allow anonymous access, the code fetches the entity from DB, then calls gRPC to look up the university name, then calls gRPC again to check user permissions — only to return "not allowed" for anonymous users. For unauthenticated requests, two unnecessary gRPC calls are made before returning an empty result.

**Fix:** Short-circuit early if `userEmail` is empty/null before making any gRPC calls. Return an empty success response (consistent with the anonymous access UX) without hitting the Core service.

---

### L-3 — Logging uses `ex.Message` instead of the exception object

**Files:** `Courses.API/Controllers/CourseController.cs:34`, `LessonController.cs:34`

```csharp
_logger.LogError(ex.Message);
```

Passing only `ex.Message` to `LogError` loses the stack trace, inner exceptions, and exception type from structured logs. This makes production debugging significantly harder.

**Fix:**
```csharp
_logger.LogError(ex, "Unhandled exception in {Action}", nameof(GetUniversityCoursesAsync));
```

Passing the exception as the first argument is the idiomatic .NET logging pattern and preserves the full exception for log aggregation tools.
