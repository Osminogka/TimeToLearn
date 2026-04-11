# Authentication Service

Handles user registration, login, and JWT issuance. Publishes new user events to RabbitMQ after registration so other services can create their own user records asynchronously.

---

## Project Structure

```
Authentication/
├── Authentication.API/
│   ├── Controllers/
│   │   ├── AuthenticationController.cs   # POST /login, POST /register
│   │   └── BaseController.cs             # JWT claim helpers, exception → HTTP mapping
│   ├── AsyncDataService/
│   │   ├── IMessageBusClient.cs
│   │   └── MessageBusClient.cs           # RabbitMQ fanout publisher
│   ├── Infrastructure/
│   │   ├── Mapper.cs                     # AutoMapper: AppUser → BaseUserPublishDto
│   │   └── PrepDb.cs                     # Runs migrations, seeds roles on startup
│   ├── appsettings.json                  # Production config (K8S cluster endpoints)
│   ├── appsettings.Development.json      # Local dev config (localhost)
│   └── Program.cs                        # DI registration, middleware pipeline
├── Authentication.DL/
│   ├── Services/
│   │   ├── IAuthService.cs
│   │   └── AuthService.cs                # Login/register logic, JWT generation
│   └── Repositories/
│       ├── IUsersRepository.cs
│       └── UsersRepository.cs            # Wraps UserManager, SignInManager, RoleManager
├── Authentication.DAL/
│   ├── Contexts/
│   │   └── IdentityContext.cs            # IdentityDbContext<AppUser>
│   ├── Models/
│   │   ├── AppUser.cs                    # Extends IdentityUser (no custom fields)
│   │   └── Roles.cs                      # Constants: "Student", "Teacher"
│   ├── Dtos/
│   │   └── BaseUserPublishDto.cs         # Payload for RabbitMQ message
│   └── SideModels/
│       ├── LoginRequestModel.cs
│       ├── RegisterRequestModel.cs
│       ├── ResponseMessage.cs            # Wraps JWT token or error in all responses
│       └── TeacherModel.cs               # Unused — placeholder for future endpoint
└── Authentication.Tests/
    └── AuthenticationControllerTests.cs  # XUnit + Moq: tests register & login flows
```

---

## Architecture

### Layer Responsibilities

| Layer | Responsibility |
|-------|---------------|
| **API** | HTTP routing, request/response mapping, RabbitMQ publishing, startup wiring |
| **DL** | Business logic: credential validation, JWT generation, input validation |
| **DAL** | EF Core entities, DbContext, ASP.NET Identity schema, DTOs, request models |
| **Tests** | Unit tests with mocked repository and message bus, real service and mapper |

### Dependencies Between Layers

```
Authentication.API
    └── Authentication.DL
            └── Authentication.DAL
```

The API layer also references DAL directly for DTOs and models used in responses.

---

## Endpoints

Base route: `api/a/authentication`

### `POST /login`

**Request:** `LoginRequestModel { Email, Password }`

**Flow:**
1. Looks up user by email
2. Validates password via `SignInManager.CheckPasswordSignInAsync`
3. Generates JWT token with user claims

**Response:** `ResponseMessage { Success, Message (JWT token) }`

---

### `POST /register`

**Request:** `RegisterRequestModel { Name, Email, Password }`

**Flow:**
1. Validates name and email are non-empty and ≤ 50 characters
2. Checks email uniqueness
3. Creates `AppUser`, automatically assigns `"Student"` role (done inside `UsersRepository.CreateAsync`)
4. Generates JWT token
5. **Controller** maps `AppUser` → `BaseUserPublishDto` and publishes to RabbitMQ

**Response:** `ResponseMessage { Success, Message (JWT token) }`

---

### `POST /refresh-token`

**Auth:** Requires valid Bearer token.

**Request:** Empty body.

**Flow:**
1. Reads current user email from token claims
2. Loads user from Identity store
3. Generates a new JWT with latest role claim (from Core role lookup)

**Response:** `ResponseMessage { Success, Message (JWT token) }`

Use this endpoint after role-changing actions so the frontend can update role-based UI immediately without forcing re-login.

> Note: RabbitMQ publishing is done in the controller after the service returns — not inside `AuthService`.

---

## JWT Tokens

- **Algorithm:** HMAC SHA256
- **Expiry:** 30 days (hardcoded)
- **Claims included:**
  - `ClaimTypes.Name` = `UserName` (used by `BaseController.getUserEmail()`)
  - `ClaimTypes.Email` = `Email`
  - `ClaimTypes.Role` = each assigned role
- **Signing key:** read from `Jwt:Key` in config; same key is shared across all services
- Issuer and audience validation are disabled

---

## RabbitMQ Integration

- **Exchange:** `"trigger"` (fanout)
- **Message:** `BaseUserPublishDto` serialized to JSON
  ```csharp
  public class BaseUserPublishDto
  {
      public Guid OriginalId { get; set; }   // AppUser.Id cast to Guid
      public string Username { get; set; }
      public string Email { get; set; }
      public string PhoneNumber { get; set; }
      public string Event { get; set; }      // always "BaseUser_Published"
  }
  ```
- `MessageBusClient` is registered as a **Singleton** (one long-lived AMQP connection per process)
- Connection failure on publish is caught and logged — registration still succeeds if RabbitMQ is unavailable

---

## Database

- Uses ASP.NET Core Identity schema via `IdentityDbContext<AppUser>`
- No custom tables — all stored in standard Identity tables (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, etc.)
- `AppUser` extends `IdentityUser` with no additional properties
- EF migrations assembly is `Authentication.API`
- Migrations run automatically on startup via `PrepDb.PrepMemberRoles()`

### Seeded Data (on startup)

| Role | Description |
|------|-------------|
| `Student` | Default role assigned on registration |
| `Teacher` | Available for future role-assignment endpoint |

---

## Configuration

### Production (`appsettings.json`)
```
DB:       Server=mssql-auth-clusterip-srv,1433; Initial Catalog=Accounts
RabbitMQ: rabbitmq-clusterip-srv:5672
JWT Key:  EUt719k5GENP1pWWhrmyDldHPaKXyIa9yImWhPuqHBUlgZ10Fk
```

### Development (`appsettings.Development.json`)
```
DB:       Server=localhost,1435; Initial Catalog=AccountsTest
RabbitMQ: localhost:5672
JWT Key:  same as above
```

---

## DI Lifetimes

| Service | Lifetime | Reason |
|---------|----------|--------|
| `IUsersRepository` | Transient | Per-request, holds EF DbContext |
| `IAuthService` | Transient | Per-request |
| `IMessageBusClient` | **Singleton** | Maintains persistent AMQP connection |

---

## Tests

**File:** `Authentication.Tests/AuthenticationControllerTests.cs`  
**Framework:** XUnit + Moq

Mocked: `IUsersRepository`, `IMessageBusClient`  
Real: `AuthService`, AutoMapper, `IConfiguration` (with hardcoded JWT key)

| Test | Verifies |
|------|---------|
| `CanUserRegister()` | Register flow returns `Success = true` with a JWT token |
| `CanUserLogin()` | Login flow returns `Success = true` with a JWT token |
