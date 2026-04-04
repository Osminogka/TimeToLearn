# Task: Multi-University Role Support

## Goal

Allow users to join multiple universities. A user is globally either a **student** or a **teacher** (mutually exclusive, as today). Each university membership is tracked with a dedicated per-university enrollment record — enabling future per-university data such as GPA for students or department/courses for teachers.

---

## Design Decisions (confirmed)

| Question | Answer |
|----------|--------|
| Can a user be both student and teacher in the same app? | No — globally one role |
| Teacher verification (Degree/IsVerified) | Global, not per-university |
| `/student/become` endpoint | Repurpose — switches a teacher back to student (clears Teacher record + TeacherEnrollments) |
| How many directors per university? | One director per university; but one user can now direct **multiple** universities |

---

## Root Cause of Current Limitation

The implicit EF join table behind `BaseUser.ICollection<University> Universities` has no role column — it stores membership only. There is no way to know *how* (as student or teacher) a user is affiliated with a specific university. Additionally, `BaseUser.UniversityDirector` is configured as a one-to-one, blocking a user from directing more than one university.

---

## New Data Model

### Remove

| What | Why |
|------|-----|
| `Student` entity (`Core.DAL/Models/Student.cs`) | Empty marker entity, no longer needed |
| `BaseUser.StudentId?` and `BaseUser.Student` nav | Replaced by absence of TeacherId (implicit student) |
| `BaseUser.ICollection<University> Universities` | Replaced by explicit enrollment tables |
| `University.ICollection<BaseUser> Members` | Same — membership is now inferred from enrollments |

### Keep (unchanged)

- `Teacher` entity — global credential (`Degree`, `IsVerified`, `BaseUserId`)
- `BaseUser.TeacherId?` and `BaseUser.Teacher` nav — presence means user is a teacher
- `University.DirectorId` and `University.Director` nav — single director per university
- `EntryRequest` — no structural change needed; role is always inferred from user's global state

### Add

#### `StudentEnrollment` (`Core.DAL/Models/StudentEnrollment.cs`)

```csharp
public class StudentEnrollment : BaseEntity
{
    public long BaseUserId { get; set; }
    public long UniversityId { get; set; }

    // Extensible: add GPA, enrollment date, status, etc. here later

    public BaseUser BaseUser { get; set; }
    public University University { get; set; }
}
```

#### `TeacherEnrollment` (`Core.DAL/Models/TeacherEnrollment.cs`)

```csharp
public class TeacherEnrollment : BaseEntity
{
    public long BaseUserId { get; set; }
    public long UniversityId { get; set; }

    // Extensible: add department, title, etc. here later

    public BaseUser BaseUser { get; set; }
    public University University { get; set; }
}
```

### Modified

#### `BaseUser`

```
Remove:  long? StudentId
Remove:  Student? Student
Remove:  ICollection<University> Universities
Add:     ICollection<StudentEnrollment> StudentEnrollments
Add:     ICollection<TeacherEnrollment> TeacherEnrollments
Change:  University? UniversityDirector  →  ICollection<University> DirectingUniversities
```

#### `University`

```
Remove:  ICollection<BaseUser> Members
Add:     ICollection<StudentEnrollment> StudentEnrollments
Add:     ICollection<TeacherEnrollment> TeacherEnrollments
```

#### `DataContext`

- Remove the EF fluent config for the old `BaseUser ↔ University` many-to-many join table
- Add `DbSet<StudentEnrollment>` and `DbSet<TeacherEnrollment>`
- Configure unique index on `(BaseUserId, UniversityId)` for both enrollment tables
- Update `BaseUser.UniversityDirector` from `.WithOne()` to `.WithMany()` config

---

## Membership Logic After Refactor

A user "belongs to" a university if **any** of these is true:
- A `StudentEnrollment` row exists for (userId, universityId)
- A `TeacherEnrollment` row exists for (userId, universityId)
- `University.DirectorId == userId`

Helper query pattern used across services:
```csharp
bool isMember = await context.StudentEnrollments.AnyAsync(e => e.BaseUserId == userId && e.UniversityId == universityId)
             || await context.TeacherEnrollments.AnyAsync(e => e.BaseUserId == userId && e.UniversityId == universityId)
             || university.DirectorId == userId;
```

---

## Service Changes

### `StudentService`

| Method | Change |
|--------|--------|
| `BecomeAStudentAsync` | Repurpose: only valid when user is currently a teacher (`TeacherId != null`). Delete `Teacher` record, delete all `TeacherEnrollment` rows for this user, set `BaseUser.TeacherId = null`. Users who are already students get an error. |
| `SendRequestToBecomeStudentOfUniversity` | Replace `StudentId != null` check with `TeacherId == null` (implicit student). Check `StudentEnrollments` for duplicate instead of `Universities` |
| `EntryUniversityAsync` | Same guard update. On success: create `StudentEnrollment` instead of adding to `Universities` collection |

### `TeacherService`

| Method | Change |
|--------|--------|
| `BecomeTeacherAsync` | Only valid when user is currently a student (`TeacherId == null`). Delete all `StudentEnrollment` rows for this user (role switch clears university ties). Delete pending `EntryRequest` rows. Create `Teacher` record as today. |
| `VerifyStatusAsync` | No change |
| `SendRequestToBecomeTeacherOfUniversity` | Replace `Universities` membership check with `TeacherEnrollments` check |

### `DirectorService`

| Method | Change |
|--------|--------|
| `InviteStudentToUniversityAsync` | Replace `Universities` membership check with `StudentEnrollments` check |
| `InviteTeacherToUniversityAsync` | Replace `Universities` membership check with `TeacherEnrollments` check |
| `AcceptEntryRequestAsync` | Check `StudentId == null && TeacherId == null` guard becomes `TeacherId == null` check (for student) or `TeacherId != null` (for teacher). On success: create appropriate enrollment record instead of adding to `Universities`. Lookup which enrollment to create by checking user's global role |
| `RemoveMemberFromUniversityAsync` | Replace `university.Members` lookup with enrollment table lookup |

### `BaseUserService`

| Method | Change |
|--------|--------|
| `GetInvitesAsync` | Replace `user.Universities.Any(u => u.Id == er.UniversityId)` check with enrollment table check |
| `AcceptInviteAsync` | On success: create `StudentEnrollment` or `TeacherEnrollment` based on user's global role instead of adding to `Universities` |
| `LeaveUniversityAsync` | Delete from `StudentEnrollment` or `TeacherEnrollment` (whichever exists) instead of `user.Universities.Remove()` |

### `UniversityService`

| Method | Change |
|--------|--------|
| `GetTeachersAsync` | Query `TeacherEnrollments` instead of filtering `Members` |
| `GetStudentsAsync` | Query `StudentEnrollments` instead of filtering `Members` |
| `CreateUniversityAsync` | After creating university: no longer adds to `Universities` collection; director relationship is already captured via `University.DirectorId` |

### `GrpcUserinfoService`

| Method | Change |
|--------|--------|
| `GetInfoForTopic` | `hasPermission` check: query `StudentEnrollments` OR `TeacherEnrollments` OR `DirectorId` match |

---

## Repository Registration

Add two new `IBaseRepository<T>` registrations in `Program.cs`:
```csharp
services.AddTransient<IBaseRepository<StudentEnrollment>, BaseRepository<StudentEnrollment>>();
services.AddTransient<IBaseRepository<TeacherEnrollment>, BaseRepository<TeacherEnrollment>>();
```

Remove registration for `IBaseRepository<Student>`.

---

## API / Controller Changes

### `StudentController`

- `GET /become` — keep endpoint, semantics change: "switch back to student" (only callable by a teacher)
- All other endpoints remain; internal service logic changes transparently

### No other controller changes needed

---

## Migration

A new EF Core migration is required (no data migration — fresh database):

1. Drop the implicit `BaseUser_Universities` join table
2. Drop `Students` table
3. Drop `BaseUser.StudentId` column
4. Add `StudentEnrollments` table
5. Add `TeacherEnrollments` table
6. Remove unique constraint on `University.DirectorId` if one exists (same user can now direct multiple universities)

---

## Tests to Update

| Test Class | What changes |
|------------|-------------|
| `StudentServiceTests` | Replace `BecomeStudent` tests — now tests the teacher→student switch (clears Teacher + TeacherEnrollments); update membership checks to use enrollments |
| `TeacherServiceTest` | Update `BecomeTeacher` — no student cleanup, expect student enrollments deleted |
| `DirectorServiceTests` | Update invite/accept/remove to use enrollment tables |
| `BaseUserServiceTest` | Update invite accept/reject to create enrollment records |
| `UniversityServiceTests` | `GetTeachers`/`GetStudents` now query enrollment tables |
| `GrpcServiceTest` | `GetInfoForTopic` — membership via enrollment tables |

---

## Out of Scope

- GPA field on `StudentEnrollment` (stub the table now, populate later)
- Teacher department/title on `TeacherEnrollment` (same)
- Director transfer (separate task)
