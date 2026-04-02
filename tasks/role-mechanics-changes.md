# Role Mechanics Changes — Core Service

Summary of all changes made to fix the role mechanics issues identified in `role-mechanics-analysis.md`.

---

## 1. Removed `IsTeacher` Flag from `BaseUser`

**Files:** `Core.DAL/Models/BaseUser.cs`, `Core.API/Infrastructure/Mapper.cs`, `Core.DL/Services/GeneralUserInfoService.cs`, `Core.DL/Services/UniversityService.cs`, `Core.DL/Services/TeacherService.cs`, `Core.DL/Services/DirectorService.cs`, `Core.DL/Services/StudentService.cs`

Removed the redundant `IsTeacher` bool from `BaseUser`. All checks now use `TeacherId != null` to determine teacher status, matching how student status was already checked (`StudentId != null`). The `ReadBaseUserDto.IsTeacher` field is preserved for the API response and is computed via AutoMapper from `TeacherId != null`.

> **Migration required:** `dotnet ef migrations add RemoveIsTeacherFlag` before deploying.

---

## 2. Fixed Orphaned Records on Role Switch

**Files:** `Core.DL/Services/StudentService.cs`, `Core.DL/Services/TeacherService.cs`

When switching from teacher → student, the old `Teacher` row is now deleted before creating the new `Student` row. When switching from student → teacher, the old `Student` row is deleted before creating the new `Teacher` row. Previously these rows were left in the database with no reference pointing to them.

---

## 3. Wrapped Role Switch in Database Transactions

**Files:** `Core.DL/Services/StudentService.cs`, `Core.DL/Services/TeacherService.cs`

Both `BecomeAStudentAsync` and `BecomeTeacherAsync` now wrap all their operations (delete old role, clear memberships, insert new role, update user) in a single database transaction. If any step fails, all changes are rolled back. Uses `context.Database.IsRelational()` to gracefully skip transactions on InMemory (used in tests).

---

## 4. Clean Up University Memberships on Role Switch

**Files:** `Core.DL/Services/StudentService.cs`, `Core.DL/Services/TeacherService.cs`

When a user switches roles, they are now removed from all universities and all pending entry requests are deleted. A user who was a student-member of a university must re-apply as a teacher after switching, and vice versa.

---

## 5. Added `IsOpened` Check for Teacher University Requests

**File:** `Core.DL/Services/TeacherService.cs`

`SendRequestToBecomeTeacherOfUniversity` now checks `university.IsOpened == true`, matching the same gate that already existed for students. Teachers can no longer send requests to closed universities.

---

## 6. Added Role Validation to `AcceptEntryRequest`

**File:** `Core.DL/Services/DirectorService.cs`

Before adding a user to a university, `AcceptEntryRequestAsync` now verifies the user has a role assigned (`StudentId != null || TeacherId != null`). A director can no longer approve a request from someone who switched away from their role after submitting the request.

---

## 7. Added Leave University and Remove Member Endpoints

**Files:** `Core.API/Controllers/BaseUserController.cs`, `Core.API/Controllers/DirectorController.cs`, `Core.DL/Services/DirectorService.cs`, `Core.DL/Services/IDirectorService.cs`

- `DELETE api/u/user/university/{universityName}/leave` — a member can leave a university. Directors cannot leave their own university (they must transfer directorship first). The service method already existed but had no controller endpoint.
- `DELETE api/u/director/members/remove` — a director can remove any member from their university (except themselves).

---

## 8. Changed GET to POST for State-Changing Actions

**Files:** `Core.API/Controllers/StudentController.cs`, `Core.API/Controllers/TeacherController.cs`

| Before | After |
|--------|-------|
| `GET /api/u/student/become` | `POST /api/u/student/become` |
| `GET /api/u/student/request/{name}` | `POST /api/u/student/request/{name}` |
| `GET /api/u/student/entry/{name}` | `POST /api/u/student/entry/{name}` |
| `GET /api/u/teacher/become` | `POST /api/u/teacher/become` |

---

## 9. Fixed Pre-Existing Bugs Found During Testing

**Files:** `Core.DL/Services/BaseUserService.cs`, all test files in `Core.Tests/`

- `GetInvitesAsync` was missing `.Include(obj => obj.Universities)`, causing a `NullReferenceException` when filtering invites for universities the user already belongs to.
- All test files referenced `IsTeacher` (removed) and `UniversityId` (never existed on `BaseUser`) in object initializers and assertions. Tests now use `TeacherId` to seed teacher users and check university membership through the `Universities` navigation collection.
- `UniversityServiceTests` constructor fixed to add members via the `Members` collection on the university entity rather than a nonexistent shadow property.
- `BaseUserServiceTest` fixed to pass `IBaseRepository<University>` to `BaseUserService` constructor (4th param was missing).
- `StudentServiceTests` and `TeacherServiceTest` updated to pass the new repository parameters added to their respective services.

---

## Test Results

All 39 tests pass across all services after the changes:

| Project | Tests |
|---------|-------|
| Core.Tests | 28 passed |
| Forums.Tests | 8 passed |
| Authentication.Tests | 2 passed |
| Courses.Tests | 1 passed |
