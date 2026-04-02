# Forums Service

Manages university forum topics, comments, and likes/dislikes. Like the Courses service, it has no local user or university tables — all identity and access control is delegated to the Core service via gRPC.

---

## Project Structure

```
Forums/
├── Forums.API/
│   ├── Controllers/
│   │   ├── BaseController.cs         # JWT claim helpers, exception → HTTP mapping
│   │   └── TopicController.cs        # Topics + likes/dislikes (api/f/topics)
│   │   └── CommentController.cs      # Comments + likes/dislikes (api/f/comments)
│   ├── Infrastructure/
│   │   └── Mapper.cs                 # AutoMapper: gRPC types → domain models, Topic → ReadTopicDto
│   ├── appsettings.json              # Production config (K8S cluster endpoints)
│   ├── appsettings.Development.json  # Local dev config
│   └── Program.cs                    # DI registration, middleware pipeline
├── Forums.DL/
│   ├── Grpc/
│   │   ├── IUserInfoClient.cs        # gRPC client interface
│   │   └── UserInfoClient.cs         # gRPC client → Core service (port 666)
│   ├── Proto/
│   │   └── userinfo.proto            # Shared proto definition (mirrors Core service)
│   ├── Repositories/
│   │   ├── IBaseRepository.cs        # Generic CRUD interface
│   │   └── BaseRepository.cs         # EF Core implementation
│   └── Services/
│       ├── ITopicService.cs / TopicService.cs
│       └── ICommentService.cs / CommentService.cs
├── Forums.DAL/
│   ├── Context/
│   │   └── DataContext.cs            # EF Core DbContext; polymorphic Like/Dislike relationships
│   ├── Models/
│   │   ├── BaseEntity.cs             # Abstract base: long Id
│   │   ├── Record.cs                 # Abstract base for Topic & Comment: CreatorId, UniversityId
│   │   ├── Topic.cs                  # Title, Content, LikesOverall, DislikesOverall, timestamps
│   │   ├── Comment.cs                # PostId, IsTopic, Content, timestamps
│   │   ├── Like.cs                   # UserId, PostId, IsTopic (polymorphic)
│   │   └── Dislike.cs                # UserId, PostId, IsTopic (polymorphic)
│   ├── Dtos/
│   │   ├── CreateTopicDto.cs         # Title, Content, UniversityName
│   │   ├── ReadTopicDto.cs           # Includes CreatorName (gRPC), Likes, Dislikes counts
│   │   ├── CreateCommentDto.cs       # PostId, IsTopic, UniversityName, Content
│   │   └── ReadCommentDto.cs         # CreatorName (gRPC), Content, LikesOverall, DislikesOverall
│   └── SideModels/
│       ├── ResponseMessage.cs        # Success + Message
│       ├── ResponseArray.cs          # Success + Message + List<T>
│       └── UserInfoForTopic.cs       # gRPC result: UserId, UniversityId, IsAllowed
└── Forums.Tests/
    ├── TopicServiceTests.cs          # 4 tests: Get, Create, Like, Dislike topics
    └── CommentServiceTests.cs        # 4 tests: Get, Create, Like, Dislike comments
```

---

## Architecture

### Layer Responsibilities

| Layer | Responsibility |
|-------|---------------|
| **API** | HTTP routing, JWT validation, startup wiring |
| **DL** | Business logic, gRPC calls, vote toggle logic, repository orchestration |
| **DAL** | EF Core entities, DbContext, DTOs, response wrappers |

### Dependencies Between Layers

```
Forums.API
    └── Forums.DL
            └── Forums.DAL
```

### Generic Repository

All persistence goes through `IBaseRepository<T>` / `BaseRepository<T>`:

```csharp
AddAsync(T)
UpdateAsync(T)             // fetches by ID first, then SetValues
DeleteAsync(T)
DeleteRangeAsync(List<T>)
GetAllAsync()
GetByIdAsync(long id)
Where(Expression<Func<T, bool>>)
SingleOrDefaultAsync(Expression<Func<T, bool>>)
GetContext()               // exposes DataContext for .Include() chains
```

Four repositories registered: `IBaseRepository<Topic>`, `IBaseRepository<Comment>`, `IBaseRepository<Like>`, `IBaseRepository<Dislike>`, all `Transient`.

---

## Data Model

### Inheritance Hierarchy

```
BaseEntity (long Id)
    └── Record (+ CreatorId, UniversityId)
            ├── Topic
            └── Comment
```

### Entities

**`Topic`**
- `TopicTitle` (max 30), `TopicContent` (max 1000)
- `LikesOverall`, `DislikesOverall` — denormalized counters, updated on every vote
- `CreatedAt`
- Navigation: `Likes`, `Dislikes`

**`Comment`**
- `PostId` (long) — ID of the parent Topic or Comment
- `IsTopic` (bool) — disambiguates what `PostId` refers to
- `CommentContent` (max 1000), `CreatedAt`
- Navigation: `Likes`, `Dislikes`

**`Like` / `Dislike`** — polymorphic votes
- `UserId` (long), `PostId` (long), `IsTopic` (bool)
- Navigation: `Topic?`, `Comment?` (only one is set, based on `IsTopic`)

### Polymorphic Vote Relationships

`Like.PostId` and `Dislike.PostId` each have **two foreign key constraints** in `DataContext.OnModelCreating` — one pointing to `Topic` and one to `Comment`. The `IsTopic` flag is the application-level discriminator; EF does not enforce exclusivity between the two FKs.

> **Note:** `Topic` stores aggregate vote counts (`LikesOverall` / `DislikesOverall`). `Comment` does **not** — comment vote counts are computed from the `Likes`/`Dislikes` collections at read time.

---

## Endpoints

### `TopicController` — `api/f/topics`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/{universityName}/{page}` | Optional | Paginated topics for a university (10/page) |
| POST | `/create` | Required | Create topic |
| POST | `/like/{topicId}` | Required | Toggle like on topic |
| POST | `/dislike/{topicId}` | Required | Toggle dislike on topic |

### `CommentController` — `api/f/comments`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/{isTopic}/{recordId}/{page}` | Optional | Paginated comments on a topic or replies to a comment |
| POST | `/create` | Required | Create comment or reply |
| POST | `/like/{commentId}` | Required | Toggle like on comment |
| POST | `/dislike/{commentId}` | Required | Toggle dislike on comment |

**Pagination page sizes:**
- Topics: 10 per page
- Comments on a topic: 10 per page
- Replies to a comment: 3 per page

---

## Vote Toggle Logic

The same toggle pattern is used for likes and dislikes on both topics and comments:

**Like:**
1. If already liked → delete the like, decrement `LikesOverall` (topics only)
2. Else if disliked → delete the dislike, decrement `DislikesOverall` (topics only), then add like, increment `LikesOverall`
3. Else → add like, increment `LikesOverall`

**Dislike:** same but inverted.

Topic counters are persisted to the `Topic` row. Comment vote counts are **not stored** — they are derived from `Likes.Count()` / `Dislikes.Count()` when building `ReadCommentDto`.

---

## Authorization Model

Every operation calls the Core service via gRPC. No roles are stored locally.

| Operation | Check |
|-----------|-------|
| Read topics / comments | `UserInfoForTopic.IsAllowed == true` |
| Create topic / comment | `IsAllowed == true` |
| Like / Dislike | `IsAllowed == true` |

Anonymous users receive empty result sets rather than 401/403 errors on read operations.

---

## gRPC Integration

**Client:** `UserInfoClient` (Scoped) calls Core service at `GrpcUsersApi` config endpoint.

| gRPC Method | When Called | Returns |
|-------------|------------|---------|
| `GetInfoForTopic(universityName, email)` | Every operation | `UserInfoForTopic { UserId, UniversityId, IsAllowed }` |
| `GetUniversityName(universityId)` | Reads and votes that start from an entity ID | University name string |
| `GetUserName(userId)` | Populating `CreatorName` in read DTOs | Username string |

gRPC failures are caught, logged to console, and return `null` — callers treat null as "not allowed."

---

## Configuration

### Production (`appsettings.json`)
```
DB:          Server=mssql-forums-clusterip-srv,1433; Initial Catalog=Data
gRPC:        http://users-clusterip-srv:666
JWT Key:     shared with all services
RabbitMQ:    rabbitmq-clusterip-srv:5672  (configured but unused)
```

### Development (`appsettings.Development.json`)
```
DB:          Server=localhost,1436; Initial Catalog=DataTest
gRPC:        not configured (omitted)
```

---

## DI Lifetimes

| Service | Lifetime |
|---------|----------|
| `IBaseRepository<Topic/Comment/Like/Dislike>` | Transient |
| `ITopicService` | Transient |
| `ICommentService` | Transient |
| `IUserInfoClient` | **Scoped** |

---

## Tests

Tests use an **in-memory SQLite database** — no mocks for repositories. Only `IUserInfoClient` is mocked.

| Test Class | Setup | Tests |
|------------|-------|-------|
| `TopicServiceTests` | 2 topics (UniversityId 1 & 2); gRPC returns allowed only for "DKU"/UniversityId=1 | GetTopics, CreateTopic, LikeTopic, DislikeTopic |
| `CommentServiceTests` | 1 topic, 2 comments on it, 1 reply to first comment; gRPC returns allowed for "DKU" | GetComments, CreateComment, LikeComment, DislikeComment |

---

## Key Implementation Notes

- **No RabbitMQ usage** — configured in `appsettings.json` but nothing is published or consumed
- **Comment vote counts are not persisted** — unlike topics, comments derive like/dislike counts from navigation collections at read time
- **`CreatorId` and `UniversityId`** are never sent by the client — always sourced from the gRPC reply
- **Replies are recursive in structure** but not in retrieval — fetching comments on a comment returns only one level deep
