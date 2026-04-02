# Forums Service — Issues & Fixes

Analysis of `Forums.API`, `Forums.DL`, and `Forums.DAL`. Issues ordered by severity.

---

## 1. `CommentController.cs` is missing (Critical)

**Location:** `Forums.API/Controllers/` — file does not exist.

`CommentService` is fully implemented and registered in DI, but there is no HTTP controller exposing its endpoints. All comment-related routes (`api/f/comments`) are unreachable.

**Fix:** Create `CommentController.cs` mirroring `TopicController.cs`, exposing:
- `GET /{isTopic}/{recordId}/{page}` → `_commentService.GetCommentsAsync(...)`
- `POST /create` → `_commentService.CreateCommentAsync(...)`
- `POST /like/{commentId}` → `_commentService.LikeCommentAsync(...)`
- `POST /dislike/{commentId}` → `_commentService.DislikeCommentAsync(...)`

---

## 2. NullReferenceException on anonymous `GET /topics` (Critical)

**Location:** `Forums.API/Controllers/BaseController.cs:14`, called from `TopicController.cs:28`

```csharp
// BaseController.cs:14
return HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name).Value;
//                                            ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
//                                            returns null for anonymous users → .Value throws NRE
```

`GetUniversityTopics` is `[AllowAnonymous]`. Anonymous requests have no `ClaimTypes.Name` claim, so `FirstOrDefault` returns `null`, and `.Value` throws `NullReferenceException`. This becomes a 500 response instead of returning topics.

**Fix:**
```csharp
protected string getUserEmail()
{
    return HttpContext.User.Claims
        .FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? string.Empty;
}
```

---

## 3. gRPC null result not guarded before `.IsAllowed` access (Critical)

**Location:** `TopicService.cs:43`, `CommentService.cs:55`

```csharp
var reply = await _grpcClient.GetUserInfoForTopic(universityName, userEmail);
if (!reply.IsAllowed)   // reply can be null when gRPC throws — NullReferenceException
```

`UserInfoClient.GetUserInfoForTopic` returns `null` on any exception (line 37–41 in `UserInfoClient.cs`). The callers do not null-check before accessing `reply.IsAllowed`.

**Fix:** Add a null guard before accessing the reply:
```csharp
var reply = await _grpcClient.GetUserInfoForTopic(universityName, userEmail);
if (reply == null || !reply.IsAllowed)
    return response;
```
Same fix needed in `CommentService.GetCommentsAsync`, `CreateCommentAsync`, `LikeCommentAsync`, `DislikeCommentAsync`.

---

## 4. `ReadTopicDto.CreatorName` is always null (High)

**Location:** `Forums.API/Infrastructure/Mapper.cs:21-25`, `Forums.DAL/Dtos/ReadTopicDto.cs:9`

The AutoMapper profile maps `Topic → ReadTopicDto` but does not map `CreatorName`. The field requires a gRPC call (`GetUserName(creatorId)`) that AutoMapper cannot perform. Every topic response returns `CreatorName = null`.

**Fix:** Abandon AutoMapper for topic reads. In `TopicService.GetUniversityTopicsAsync`, build `ReadTopicDto` manually with a loop (matching what `CommentService.GetCommentsAsync` already does), calling `_grpcClient.GetUserName(topic.CreatorId)` per topic. Remove the `Topic → ReadTopicDto` mapping from `MappingProfile`.

---

## 5. Vote counter desync — no transactions (High)

**Location:** `TopicService.cs:118-145`, `TopicService.cs:179-206`

The vote toggle makes 2–3 separate `SaveChangesAsync` calls with no wrapping transaction:
```
DeleteAsync(like)       → SaveChangesAsync #1
UpdateAsync(topic)      → SaveChangesAsync #2
```
If the process crashes or the DB times out between these saves, counters (`LikesOverall` / `DislikesOverall`) diverge from the actual row count in `Likes`/`Dislikes`.

**Fix:** Wrap each vote method body in a `DbContext` transaction:
```csharp
using var tx = await _topicRepository.GetContext().Database.BeginTransactionAsync();
// ... all operations ...
await tx.CommitAsync();
```

---

## 6. Race condition in vote toggle — no unique constraint (High)

**Location:** `TopicService.cs:114`, `CommentService.cs:153`

The like/dislike check is `SingleOrDefaultAsync` followed by `AddAsync` — a classic check-then-act race. Two concurrent requests from the same user can both pass the "not yet liked" check and both insert a `Like` row, doubling the counter.

**Fix:** Add a unique index on `(UserId, PostId, IsTopic)` in `DataContext.OnModelCreating`:
```csharp
modelBuilder.Entity<Like>()
    .HasIndex(e => new { e.UserId, e.PostId, e.IsTopic })
    .IsUnique();
modelBuilder.Entity<Dislike>()
    .HasIndex(e => new { e.UserId, e.PostId, e.IsTopic })
    .IsUnique();
```
Handle the resulting `DbUpdateException` at the service layer.

---

## 7. Polymorphic FK — last config wins, Topic FK silently dropped (High)

**Location:** `Forums.DAL/Context/DataContext.cs:19-37`

```csharp
modelBuilder.Entity<Like>()
    .HasOne(e => e.Topic).WithMany(e => e.Likes).HasForeignKey(e => e.PostId);  // overwritten ↓

modelBuilder.Entity<Like>()
    .HasOne(e => e.Comment).WithMany(e => e.Likes).HasForeignKey(e => e.PostId); // wins
```

Both relationships declare `PostId` as the FK column. EF Core's last configuration overwrites the first, so only the `Like → Comment` FK constraint is generated in the migration. The `Like → Topic` FK is silently discarded. A `Like` with `IsTopic = true` and a nonexistent `topicId` will not violate any DB constraint.

**Fix:** Use separate FK columns — add `TopicId` and `CommentId` nullable columns to `Like`/`Dislike`, or switch to a table-per-hierarchy or shadow-property design. At minimum, document the limitation and enforce the invariant in the service layer (which it does partially via `IsTopic`).

---

## 8. N+1 sequential gRPC calls in `GetCommentsAsync` (Medium)

**Location:** `CommentService.cs:75`

```csharp
foreach (Comment comment in comments)
{
    tempReadCommentDto.CreatorName = await _grpcClient.GetUserName(comment.CreatorId); // per comment
}
```

For a page of 10 comments, 10 sequential gRPC calls are made. This multiplies latency linearly with page size. The same issue exists (once fixed) for `GetUniversityTopicsAsync` topics.

**Fix:** Deduplicate `CreatorId`s, batch or parallelize lookups. Minimal fix using `Task.WhenAll`:
```csharp
var nameMap = await Task.WhenAll(
    comments.Select(c => c.CreatorId).Distinct()
            .Select(async id => (id, name: await _grpcClient.GetUserName(id))));
var names = nameMap.ToDictionary(x => x.id, x => x.name);
```

---

## 9. `CreateCommentAsync` does not verify `PostId` belongs to the claimed university (Medium)

**Location:** `CommentService.cs:98-108`

Authorization is checked with `createCommentDto.UniversityName` (client-supplied), but `PostId` is not validated to belong to that university. A user can:
1. Know a `topicId` from a different university
2. Pass their own `universityName` in the DTO
3. Pass authorization (they are a member of their own university)
4. Create a comment on a topic from a different university

**Fix:** After fetching `doesRecordExist`, verify `doesRecordExist.UniversityId == reply.UniversityId`.

---

## 10. `int` vs `long` truncation in `ReadTopicDto` (Low)

**Location:** `Forums.DAL/Dtos/ReadTopicDto.cs:11-12`, `Forums.DAL/Models/Topic.cs:17-19`

`Topic.LikesOverall` and `Topic.DislikesOverall` are `long`. `ReadTopicDto.Likes` and `ReadTopicDto.Dislikes` are `int`. AutoMapper silently truncates the value if it exceeds `int.MaxValue` (~2.1 billion).

**Fix:** Change `ReadTopicDto.Likes` and `ReadTopicDto.Dislikes` to `long`.

---

## 11. `UpdateAsync` fetches entity again unnecessarily (Low)

**Location:** `Forums.DL/Repositories/BaseRepository.cs:36`

```csharp
public async Task<int> UpdateAsync(T entity)
{
    var oldEntity = await _context.FindAsync<T>(entity.Id); // extra DB round-trip
    _context.Entry(oldEntity).CurrentValues.SetValues(entity);
    return await _context.SaveChangesAsync();
}
```

The entity passed in was already fetched and is already tracked in the same `DbContext` (same `Transient` repository instance). `FindAsync` will return the same tracked instance from the identity map, making the extra call unnecessary. The `SetValues` still works but the round-trip is wasteful.

**Fix:** Check `_context.Entry(entity).State` — if `Modified` or `Unchanged`, call `SaveChangesAsync` directly without fetching again.

---

## Summary Table

| # | Issue | Severity | File |
|---|-------|----------|------|
| 1 | `CommentController.cs` missing | Critical | `Forums.API/Controllers/` |
| 2 | NRE on anonymous topic GET | Critical | `BaseController.cs:14` |
| 3 | gRPC null not guarded | Critical | `TopicService.cs:43`, `CommentService.cs:55` |
| 4 | `CreatorName` always null in topic responses | High | `Mapper.cs:21` |
| 5 | No transactions on vote toggle | High | `TopicService.cs:118-145` |
| 6 | Race condition — no unique vote constraint | High | `DataContext.cs` |
| 7 | Dual FK on same column, Topic FK dropped | High | `DataContext.cs:19-37` |
| 8 | N+1 gRPC calls per comment | Medium | `CommentService.cs:75` |
| 9 | Cross-university comment possible | Medium | `CommentService.cs:98` |
| 10 | `int`/`long` truncation in DTO | Low | `ReadTopicDto.cs:11` |
| 11 | `UpdateAsync` double-fetch | Low | `BaseRepository.cs:36` |
