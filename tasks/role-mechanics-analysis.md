# Role Mechanics Analysis — Core Service

Analysis of the student/teacher role system: what is wrong, what is risky, and concrete improvement suggestions.

---

## 1. Orphaned Records on Role Switch

**Problem:** When a user switches roles, the old role's database record is never deleted.

- `BecomeAStudentAsync` sets `user.TeacherId = null` and `user.IsTeacher = false` — but the `Teacher` row stays in the DB.
- `BecomeTeacherAsync` sets `user.StudentId = null` — but the `Student` row stays in the DB.

**Consequence:** Every role switch leaks a row. Over time the `Student` and `Teacher` tables fill with orphaned records that are no longer reachable through any navigation property.

A secondary consequence: if a user switches back (teacher → student → teacher), a **new** `Teacher` record is created. Their old `Degree` and `IsVerified` status from the previous tenure are silently lost and a fresh unverified record starts.

**Fix:** Delete the old role record before/during the role switch. Wrap both operations (delete old + insert new + update user) in a single EF transaction.

---

## 2. No Transaction Around Role Creation

**Problem:** `BecomeAStudentAsync` and `BecomeTeacherAsync` each do two separate `await` calls:
1. `AddAsync(student/teacher)` — inserts role row
2. `UpdateAsync(user)` — updates FK on user

If the second call fails, you have an inserted `Student`/`Teacher` row with no `BaseUser.StudentId`/`TeacherId` pointing to it — orphaned immediately on creation.

**Fix:** Wrap both calls in `using var tx = await _context.Database.BeginTransactionAsync()`.

---

## 3. IsTeacher Flag vs TeacherId — Redundant and Can Desync

**Problem:** `BaseUser` has both `IsTeacher` (bool) and `TeacherId?` (nullable long). These must always be kept in sync manually. The `Student` role has no equivalent `IsStudent` flag — membership is checked by `obj.StudentId != null`. This asymmetry means:

- There are two different patterns used throughout the codebase for the same concept.
- Any bug in the flag-setting logic creates a ghost state: a user where `IsTeacher = true` but `TeacherId = null`, or vice versa.

In `BecomeAStudentAsync` the code sets `user.TeacherId = null` and `user.IsTeacher = false`. In `BecomeTeacherAsync` it only sets `user.StudentId = null` — there is no `IsStudent` flag to clear, so this is asymmetric cleanup.

**Fix:** Pick one representation and stick to it. Either use nullable FKs only (`TeacherId != null` → is teacher), or use an enum `Role { None, Student, Teacher }` field. Remove the redundant flag.

---

## 4. Teacher Verification Is Self-Service

**Problem:** `POST /teacher/verify` accepts a degree string **from the user** and immediately sets `IsVerified = true`. There is no admin/review step. The feature is named "verify" but it is actually just "self-declare a degree."

**Consequence:** Any user can become a "verified" teacher by calling one endpoint with any string. The `IsVerified` gate on university requests provides no real security.

**Fix:** Either:
- Rename it honestly (`SetDegree`) and remove the `IsVerified` gate if you don't intend real verification.
- Or introduce a real admin/director verification flow: submission creates a pending request, an admin approves/rejects it.

---

## 5. Open/Closed University Check Is Asymmetric

**Problem:** `SendRequestToBecomeStudentOfUniversity` checks `obj.IsOpened == true` — students can only request to join open universities. But `SendRequestToBecomeTeacherOfUniversity` has **no `IsOpened` check** — teachers can request to join any university, open or closed.

**Consequence:** Teachers bypass the closed/open access control entirely.

**Fix:** Add the same `IsOpened` check (or a deliberate override) to the teacher request path. Document the intention either way.

---

## 6. University Membership Is Not Cleaned Up on Role Switch

**Problem:** If a student is a member of University X and then calls `/teacher/become`, they become a teacher but remain a member of University X as a student. The university membership table has no role column — there is no indication of **in which capacity** a user is a member.

**Consequence:**
- A "teacher" can be in a university they joined as a student, with no re-verification as a teacher.
- A director inviting someone as a student sends an `EntryRequest` scoped to the student role, but nothing prevents that person from switching to teacher before accepting. The invite is role-unaware.

**Fix:** Either:
- Store a role snapshot in the join table (members table) — e.g. `MemberRole { Student, Teacher }`.
- Or kick users out of all universities when they switch roles, requiring them to re-apply in their new role.

---

## 7. AcceptEntryRequest Does Not Validate Role

**Problem:** `DirectorService.AcceptEntryRequestAsync` does not check whether the user being accepted is still a student or teacher. It just adds them to `university.Members` based on a username match against the pending `EntryRequest`.

**Consequence:** A director can approve a request from someone who has since switched roles (or has no role at all). The university ends up with a member whose actual role doesn't match why they were approved.

---

## 8. No Way to Leave a University or Remove a Member

**Problem:** There is no endpoint for:
- A user leaving a university voluntarily.
- A director removing a student or teacher from their university.

**Consequence:** University membership is permanent once granted. This is a significant gap for any real-world use.

---

## 9. GET Used for State-Changing Operations

**Problem:** The following endpoints use `GET` but mutate state:

| Endpoint | What it mutates |
|----------|----------------|
| `GET /student/become` | Creates Student record, updates BaseUser |
| `GET /teacher/become` | Creates Teacher record, updates BaseUser |
| `GET /student/request/{name}` | Creates EntryRequest |
| `GET /student/entry/{name}` | Adds user to university, deletes EntryRequests |

**Consequence:** GET requests can be triggered by browser prefetch, link previews, logging middleware, and caching proxies. A user sharing a link or a browser tab prefetch could accidentally trigger a role change.

**Fix:** Change these to `POST`. They already have auth — the only missing piece is the HTTP verb.

---

## 10. Student Has No Data

**Problem:** The `Student` entity is:
```csharp
public class Student : BaseEntity
{
    public long BaseUserId { get; set; }
    public BaseUser BaseUser { get; set; }
}
```

It carries zero domain data. Its only purpose is to hold an ID and a FK back to `BaseUser`, which is already tracked by `BaseUser.StudentId`.

**Consequence:** The `Student` table is a pure join-table / flag implemented as a full entity, adding two extra DB round-trips (insert + FK update) for every role assignment while providing nothing a nullable `BaseUser.StudentId` (or an enum field) wouldn't give you more simply.

If future requirements need student-specific data (enrollment date, GPA, etc.), the entity is justified. Otherwise it is over-engineering that creates the orphan and transaction problems described above.

---

## Summary Table

| # | Issue | Severity | Effort to fix |
|---|-------|----------|---------------|
| 1 | Orphaned Teacher/Student rows on role switch | High | Low |
| 2 | No transaction on role creation | High | Low |
| 3 | IsTeacher flag + TeacherId redundancy | Medium | Medium |
| 4 | Teacher verification is self-service | Medium | Medium–High |
| 5 | No IsOpened check for teacher requests | Medium | Low |
| 6 | University membership not cleared on role switch | Medium | Medium |
| 7 | AcceptEntryRequest ignores current role | Low | Low |
| 8 | No leave/remove university endpoint | Low | Medium |
| 9 | GET used for state-changing actions | Medium | Low |
| 10 | Student entity carries no domain data | Low | Medium |
