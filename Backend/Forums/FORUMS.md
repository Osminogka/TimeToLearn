# Forums Service

Forums is the microservice responsible for university-scoped forum discussions: topics, comments/replies, and vote reactions (likes/dislikes).

The service does not own user, university, or role data. It delegates identity and access checks to the Core service over gRPC.

---

## What The Service Handles

Primary functionality:

- Create and read topics per university
- Create and read comments on topics
- Create and read replies on comments
- Toggle likes/dislikes for topics
- Toggle likes/dislikes for comments
- Enrich read responses with author name and university role from Core (gRPC)

Boundaries:

- No local users or universities tables
- No role/permission model stored locally
- RabbitMQ settings exist in configuration but there is no producer/consumer implementation in this service

---

## High-Level Architecture

Layered architecture with three projects:

```
Forums.API  -> HTTP endpoints, auth middleware, DI wiring, EF migrations runner
Forums.DL   -> business logic, repository orchestration, gRPC client
Forums.DAL  -> EF Core model, DbContext, DTOs, response wrappers
```

Dependency direction:

```
Forums.API
	-> Forums.DL
			-> Forums.DAL
```

Key runtime integrations:

- SQL Server (EF Core)
- Core service gRPC API (`GrpcUsersApi`)
- JWT bearer auth via OpenID metadata/public key

---

## Project Structure

```
Forums/
├── Forums.API/
│   ├── Controllers/
│   │   ├── BaseController.cs      # claim extraction + centralized exception mapping
│   │   ├── TopicController.cs     # /api/f/topics
│   │   └── CommentController.cs   # /api/f/comments
│   ├── Infrastructure/
│   │   └── Mapper.cs              # AutoMapper profile for gRPC-generated types
│   ├── Migrations/
│   ├── Program.cs                 # DI setup, auth, routing, DB migrate on startup
│   ├── appsettings.json
│   └── appsettings.Development.json
├── Forums.DL/
│   ├── Grpc/
│   │   ├── IUserInfoClient.cs
│   │   └── UserInfoClient.cs
│   ├── Proto/
│   │   └── userinfo.proto         # gRPC client contract (Core service)
│   ├── Repositories/
│   │   ├── IBaseRepository.cs
│   │   └── BaseRepository.cs
│   └── Services/
│       ├── ITopicService.cs / TopicService.cs
│       └── ICommentService.cs / CommentService.cs
├── Forums.DAL/
│   ├── Context/
│   │   └── DataContext.cs
│   ├── Models/
│   │   ├── BaseEntity.cs
│   │   ├── Record.cs
│   │   ├── Topic.cs
│   │   ├── Comment.cs
│   │   ├── Like.cs
│   │   └── Dislike.cs
│   ├── Dtos/
│   │   ├── CreateTopicDto.cs
│   │   ├── ReadTopicDto.cs
│   │   ├── CreateCommentDto.cs
│   │   └── ReadCommentDto.cs
│   └── SideModels/
│       ├── ResponseMessage.cs
│       ├── ResponseArray.cs
│       └── UserInfoForTopic.cs
└── Forums.Tests/
		├── TopicServiceTests.cs
		└── CommentServiceTests.cs
```

---

## API Surface

### Topic endpoints (`api/f/topics`)

| Method | Route | Auth | Behavior |
|---|---|---|---|
| GET | `/{universityName}/{page}` | AllowAnonymous | Returns paged topics if requester is allowed by Core |
| POST | `/create` | Required | Creates topic in request university |
| POST | `/like/{topicId}` | Required | Toggles like on topic |
| POST | `/dislike/{topicId}` | Required | Toggles dislike on topic |

### Comment endpoints (`api/f/comments`)

| Method | Route | Auth | Behavior |
|---|---|---|---|
| GET | `/{isTopic}/{recordId}/{page}` | Required | Returns comments for topic or replies for comment |
| POST | `/create` | Required | Creates comment or reply |
| POST | `/like/{commentId}` | Required | Toggles like on comment |
| POST | `/dislike/{commentId}` | Required | Toggles dislike on comment |

Important nuance:

- Topic reads are anonymous at controller level, but still pass through service-level authorization via gRPC.
- Comment reads are not anonymous (no `[AllowAnonymous]` on `CommentController.GetCommentsAsync`).

---

## Service Logic

### `TopicService`

Main methods:

- `GetUniversityTopicsAsync(universityName, userEmail, page)`
- `CreateTopicAsync(createDto, creatorEmail)`
- `LikeTopicAsync(topicId, creatorEmail)`
- `DislikeTopicAsync(topicId, creatorEmail)`

Behavior details:

- Page validation: rejects `page < 0`
- Uses `GetUserInfoForTopic` to verify access
- Topic page size is 10
- Enriches each topic with:
	- `CreatorName` from `GetUserName`
	- `CreatorRole` from `GetUserUniversityRole`
- Topic vote counters are persisted (`LikesOverall`, `DislikesOverall`)
- Like/dislike toggle operations run in explicit EF transaction (`BeginTransactionAsync`)

### `CommentService`

Main methods:

- `GetCommentsAsync(isTopic, recordId, userEmail, page)`
- `CreateCommentAsync(createDto, creatorEmail)`
- `LikeCommentAsync(commentId, userEmail)`
- `DislikeCommentAsync(commentId, userEmail)`

Behavior details:

- Page validation: rejects `page < 0`
- Loads parent record as:
	- Topic when `isTopic == true`
	- Comment when `isTopic == false`
- Resolves university name via gRPC and checks access via `GetUserInfoForTopic`
- Page size:
	- Comments on topic: 10
	- Replies on comment: 5
- Reads include `Likes` and `Dislikes` navigation collections
- `RepliesCount` is calculated via grouped query (`!c.IsTopic && commentIds.Contains(c.PostId)`)
- Comment likes/dislikes are toggled by adding/removing vote rows (no aggregate counter fields on `Comment`)

---

## Authorization And Identity Model

Authentication:

- JWT Bearer configured in `Program.cs`
- Token validation uses OpenID issuer + metadata endpoint
- Optional fallback RSA public key from config (`OpenId:PublicKey`)

Authorization strategy:

- Controller-level `[Authorize]` by default
- Business-level permission gate uses Core gRPC response (`GrpcTopicInfoModel.IsAllowed`)
- The Core response is director-aware (`IsDirector`) so university owners can pass the same access gate as enrolled members
- On denied access, services return unsuccessful response objects with domain message (usually not HTTP 403 directly)

Identity source of truth:

- `CreatorId` and `UniversityId` are sourced from gRPC reply, not trusted from client payload

---

## gRPC Dependencies (Core Service)

`Forums.DL/Proto/userinfo.proto` defines client-side contract:

- `GetInfoForTopic(GetInfoRequest)` -> `GrpcTopicInfoModel` (`userId`, `universityId`, `isAllowed`, `isTeacher`, `isDirector`)
- `GetUniversityName(UniversityId)` -> `UniversityName`
- `GetUserName(UserId)` -> `UserName`
- `GetUserUniversityRole(UserUniversityRequest)` -> `UserUniversityRole`

`UserInfoClient` behavior:

- Wraps generated `GrpcUsers.GrpcUsersClient`
- Uses AutoMapper for proto/domain mapping
- Catches gRPC exceptions, logs to console, and returns `null`/empty fallback

This means gRPC communication failures are effectively treated as authorization/data failure in calling services.

---

## Data Model

Inheritance:

```
BaseEntity (Id)
	-> Record (CreatorId, UniversityId)
			-> Topic
			-> Comment
```

### `Topic`

- `TopicTitle` (required, max 30)
- `TopicContent` (required, max 1000)
- `CreatedAt`
- `LikesOverall`, `DislikesOverall` (persisted aggregate counters)
- Navigations: `Likes`, `Dislikes`

### `Comment`

- `PostId` (parent topic/comment id)
- `IsTopic` (parent discriminator)
- `CommentContent` (required, max 1000)
- `CreatedAt`
- Navigations: `Likes`, `Dislikes`

### `Like` / `Dislike`

- `UserId`, `PostId`, `IsTopic`
- Optional navigations to `Topic` and `Comment`
- Unique composite index in `DataContext`:
	- `(UserId, PostId, IsTopic)` for likes
	- `(UserId, PostId, IsTopic)` for dislikes

Design note:

- The current model uses `PostId` + `IsTopic` at service level, while EF relationships are represented by optional `TopicId`/`CommentId` FKs in migration snapshot. The service logic does not rely on those FK columns to resolve target records.

---

## Repository Pattern

All data access goes through generic repository abstraction:

- `AddAsync`, `UpdateAsync`, `DeleteAsync`, `DeleteRangeAsync`
- `GetAllAsync`, `GetByIdAsync`, `Where`, `SingleOrDefaultAsync`
- `GetContext()` is exposed for advanced querying/transactions in services

Registered repositories:

- `IBaseRepository<Topic>`
- `IBaseRepository<Comment>`
- `IBaseRepository<Like>`
- `IBaseRepository<Dislike>`

All are registered as `Transient`.

---

## Configuration

Production (`Forums.API/appsettings.json`):

- SQL Server: `mssql-forums-clusterip-srv:1433`
- gRPC Core endpoint: `http://core-clusterip-srv:666`
- OpenID issuer + metadata address via auth service cluster IP
- RabbitMQ host/port present but unused by current code

Development (`Forums.API/appsettings.Development.json`):

- SQL Server: `localhost,1436` (`DataTest`)
- OpenID issuer + metadata address set to localhost auth
- No `GrpcUsersApi` entry in file (must be provided externally for gRPC calls)

---

## Dependency Injection And Runtime Pipeline

`Program.cs` wiring:

- AutoMapper assembly scanning
- gRPC channel + generated client as singletons
- `IUserInfoClient` as scoped
- Repositories + domain services as transient
- EF Core SQL Server context with migrations assembly `Forums.API`
- JWT authentication + authorization

Runtime specifics:

- `app.UseRouting()` -> `UseAuthentication()` -> `UseAuthorization()` -> `MapControllers()`
- Executes `Database.Migrate()` on startup
- `UseDefaultFiles()` enabled (but service is API-centric)

---

## Tests And Coverage Scope

`Forums.Tests` validates service-layer behavior with EF InMemory + mocked gRPC client.

`TopicServiceTests` covers:

- Get topics
- Create topic
- Like topic
- Dislike topic

`CommentServiceTests` covers:

- Get comments
- Create comment
- Like comment
- Dislike comment

Testing characteristics:

- Repository and service logic are tested without HTTP controller pipeline
- gRPC outcomes are simulated through `IUserInfoClient` mocks
- Focus is on happy-path behavior and permission gating

---

### Detailed Test Cases

TopicServiceTests (selected test methods):

- `GetTopics_ValidRequest_ReturnsOnlyUniversityTopics` — returns topics for a university when user is allowed.
- `GetTopics_EnrichesCreatorNameAndRole` — ensures `CreatorName` and `CreatorRole` are populated from gRPC.
- `GetTopics_NegativePage_Fails` — rejects negative page numbers.
- `GetTopics_NotAllowed_Fails` — returns failure when user is not allowed to view the university.
- `GetTopics_SecondPage_ReturnsEmpty` — verifies pagination returns empty for out-of-range pages.
- `CreateTopic_ValidRequest_CreatesTopicAndReturnsSuccess` — creates a topic for valid requests.
- `CreateTopic_EmptyTitle_Fails` — validation failure for empty title.
- `CreateTopic_EmptyContent_Fails` — validation failure for empty content.
- `CreateTopic_NotAllowed_Fails` — rejects creation when user lacks rights.
- `LikeTopic_ValidRequest_LikesTopicAndIncrementsCounter` — adds a like and increments aggregate counter.
- `LikeTopic_TopicNotFound_Fails` — handles missing topic.
- `LikeTopic_UniversityNotFound_Fails` — handles missing university metadata.
- `LikeTopic_NotAllowed_Fails` — rejects like when not allowed.
- `LikeTopic_SecondLike_RemovesLikeAndDecrementsCounter` — toggles like off on second call.
- `LikeTopic_WhenAlreadyDisliked_RemovesDislikeAndAddsLike` — handles switching from dislike to like.
- `DislikeTopic_ValidRequest_DislikesTopicAndIncrementsCounter` — adds a dislike and increments counter.
- `DislikeTopic_TopicNotFound_Fails` — handles missing topic for dislike.
- `DislikeTopic_UniversityNotFound_Fails` — handles missing university for dislike.
- `DislikeTopic_NotAllowed_Fails` — rejects dislike when not allowed.
- `DislikeTopic_SecondDislike_RemovesDislikeAndDecrementsCounter` — toggles dislike off on second call.
- `DislikeTopic_WhenAlreadyLiked_RemovesLikeAndAddsDislike` — handles switching from like to dislike.

CommentServiceTests (selected test methods):

- `GetComments_ForTopic_ReturnsTopicComments` — reads comments for a topic.
- `GetComments_ForComment_ReturnsReplies` — reads replies for a comment.
- `GetComments_EnrichesCreatorNameAndRole` — enriches comments with creator name and role.
- `GetComments_CountsRepliesForEachComment` — computes `RepliesCount` correctly.
- `GetComments_AfterLike_ShowsCorrectLikeCount` — reflects likes after toggling.
- `GetComments_AfterDislike_ShowsCorrectDislikeCount` — reflects dislikes after toggling.
- `GetComments_NegativePage_Fails` — rejects negative page numbers.
- `GetComments_TopicNotFound_Fails` — handles missing topic.
- `GetComments_CommentRecordNotFound_Fails` — handles missing comment record.
- `GetComments_UniversityNotFound_Fails` — handles missing university metadata.
- `GetComments_NotAllowed_Fails` — rejects read when user not allowed.
- `GetComments_SecondPage_ReturnsEmpty` — pagination behavior for comments/replies.
- `CreateComment_OnTopic_CreatesCommentAndReturnsSuccess` — creates comment on topic.
- `CreateComment_AsReply_CreatesReplyAndReturnsSuccess` — creates reply to comment.
- `CreateComment_EmptyContent_Fails` — validation failure for empty comment content.
- `CreateComment_NotAllowed_Fails` — rejects creation when user lacks rights.
- `CreateComment_TopicPostNotFound_Fails` — handles posting to missing topic.
- `CreateComment_CommentPostNotFound_Fails` — handles replying to missing comment.
- `CreateComment_UniversityIdMismatch_Fails` — rejects when university id mismatch detected.
- `LikeComment_ValidRequest_LikesCommentAndReturnsSuccess` — likes a comment and persists vote row.
- `LikeComment_CommentNotFound_Fails` — handles missing comment when liking.
- `LikeComment_UniversityNotFound_Fails` — handles missing university when liking.
- `LikeComment_NotAllowed_Fails` — rejects like when not allowed.
- `LikeComment_SecondLike_RemovesLike` — toggles like off on second call.
- `LikeComment_WhenAlreadyDisliked_RemovesDislikeAndAddsLike` — switches dislike→like.
- `DislikeComment_ValidRequest_DislikesCommentAndReturnsSuccess` — dislikes a comment and persists vote row.
- `DislikeComment_CommentNotFound_Fails` — handles missing comment for dislike.
- `DislikeComment_UniversityNotFound_Fails` — handles missing university for dislike.
- `DislikeComment_NotAllowed_Fails` — rejects dislike when not allowed.
- `DislikeComment_SecondDislike_RemovesDislike` — toggles dislike off on second call.
- `DislikeComment_WhenAlreadyLiked_RemovesLikeAndAddsDislike` — switches like→dislike.

For the full test source, see `Forums.Tests/TopicServiceTests.cs` and `Forums.Tests/CommentServiceTests.cs`.

## Summary

Forums is a focused discussion microservice with clear university scoping and centralized authorization delegated to Core via gRPC. It manages forum content and voting state, enriches responses with user metadata from Core, and persists domain data in its own SQL database. The architecture is cleanly layered, with the main operational dependency being Core service availability for both authorization and data enrichment.
