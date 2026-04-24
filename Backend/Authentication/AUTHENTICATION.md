# Authentication Service

Authentication is responsible for account registration, login, token refresh, and JWT issuance. It signs tokens with RSA keys, publishes user-created events to RabbitMQ so other services can provision their user projections, and exposes OpenID discovery/JWKS endpoints for token verification.

---

## Project Structure

```text
Authentication/
├── Authentication.API/
│   ├── Controllers/
│   │   ├── AuthenticationController.cs   # /login, /register, /refresh-token
│   │   ├── BaseController.cs             # common claim helper + exception mapping
│   │   └── WellKnownController.cs        # .well-known metadata and JWKS
│   ├── AsyncDataService/
│   │   ├── IMessageBusClient.cs
│   │   └── MessageBusClient.cs           # RabbitMQ fanout publisher
│   ├── Infrastructure/
│   │   ├── CoreRoleClient.cs             # HTTP role lookup against Core service
│   │   ├── Mapper.cs                     # AutoMapper: AppUser -> BaseUserPublishDto
│   │   ├── PrepDb.cs                     # migrations + role seed at startup
│   │   └── RsaKeyService.cs              # JWT signing/validation keys
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── Program.cs
├── Authentication.DL/
│   ├── Services/
│   │   ├── AuthService.cs
│   │   ├── IAuthService.cs
│   │   ├── ICoreRoleClient.cs
│   │   └── IRsaKeyService.cs
│   └── Repositories/
│       ├── IUserRepository.cs            # contains IUsersRepository interface
│       └── UserRepository.cs
├── Authentication.DAL/
│   ├── Contexts/
│   │   └── IdentityContext.cs            # IdentityDbContext<AppUser>
│   ├── Dtos/
│   │   └── BaseUserPublishDto.cs
│   ├── Models/
│   │   ├── AppUser.cs
│   │   └── Roles.cs
│   └── SideModels/
│       ├── LoginRequestModel.cs
│       ├── RegisterRequestModel.cs
│       ├── ResponseMessage.cs
│       └── TeacherModel.cs               # currently unused
└── Authentication.Tests/
    └── AuthenticationControllerTests.cs
```

---

## Architecture

### Layer Responsibilities

| Layer | Responsibility |
|-------|---------------|
| API | HTTP endpoints, middleware pipeline, publishing integration events, OpenID/JWKS metadata |
| DL | Authentication business logic, JWT creation, claim composition |
| DAL | Identity entities, EF context, DTOs/models used by API and DL |
| Tests | Controller-level unit tests with mocked integrations |

### Dependency Direction

```text
Authentication.API
    -> Authentication.DL
        -> Authentication.DAL
```

API also references DAL directly for request/response models and event DTOs.

---

## Services and Integrations Used

| Dependency | Purpose | Implementation |
|------------|---------|----------------|
| ASP.NET Core Identity | User management and credential verification | UserManager, SignInManager, RoleManager in UserRepository |
| SQL Server | Identity persistence | IdentityContext (IdentityDbContext<AppUser>) |
| RabbitMQ | Async user-created event publishing | MessageBusClient, fanout exchange trigger |
| Core service (HTTP) | Source of authoritative current role (Teacher/Student) | CoreRoleClient -> GET /api/u/general/{email} |
| RSA key material | JWT signing and validation | RsaKeyService + IRsaKeyService |
| AutoMapper | AppUser -> BaseUserPublishDto mapping | MappingProfile |

Role resolution is intentionally resilient: if Core is unavailable or user data has not synchronized yet, Authentication falls back to Student.

---

## HTTP Functionality

Base auth route: api/a/authentication/

### POST /login

Request: LoginRequestModel { Email, Password }

Flow:
1. Find user by email.
2. Verify password via SignInManager.CheckPasswordSignInAsync.
3. Build JWT with Name, Email, and Role claims.

Response: ResponseMessage { Success, Message } where Message is JWT when successful.

### POST /register

Request: RegisterRequestModel { Name, Email, Password }

Flow:
1. Validate name/email are non-empty and <= 50 chars.
2. Ensure email is unique.
3. Create AppUser in Identity.
4. Generate JWT.
5. Map AppUser to BaseUserPublishDto, set Event = BaseUser_Published, publish to RabbitMQ.

Response: ResponseMessage { Success, Message }.

Notes:
- Registration does not directly assign an Identity role in this service.
- Role claim in issued token is still present because it is fetched from Core (or fallback Student).

### POST /refresh-token

Auth: Bearer token required.

Flow:
1. Extract email from ClaimTypes.Email.
2. Load user from Identity.
3. Issue a fresh JWT with latest role from Core lookup.

Response: ResponseMessage { Success, Message }.

---

## OpenID and JWKS Endpoints

### GET /.well-known/jwks.json

Returns public signing key in JWKS shape (kty/use/kid/alg/n/e).

### GET /.well-known/openid-configuration

Returns minimal OpenID metadata:
- issuer
- jwks_uri
- token_endpoint
- id_token_signing_alg_values_supported = [RS256]
- response_types_supported = [token]

---

## JWT Details

- Algorithm: RS256 (RSA SHA-256)
- Expiration: 30 days
- Claims:
  - ClaimTypes.Name = AppUser.UserName
  - ClaimTypes.Email = AppUser.Email
  - ClaimTypes.Role = role from CoreRoleClient
- Issuer: OpenId:Issuer
- Validation (API middleware):
  - ValidateIssuerSigningKey = true
  - ValidateIssuer = true
  - ValidateAudience = false
  - ValidateLifetime = true

If RsaKeys are missing in config, RsaKeyService generates an ephemeral key pair at startup and logs warning/instructions. In that mode, tokens become invalid after restart.

---

## RabbitMQ Event Contract

Exchange: trigger (fanout)

Payload:

```csharp
public class BaseUserPublishDto
{
    public Guid OriginalId { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string Event { get; set; }
}
```

Current event name on registration: BaseUser_Published.

Publish failures are caught/logged and do not block successful registration responses.

---

## Startup Behavior

Program.cs configures:
- RSA key service singleton created early and reused by JWT config.
- Identity with password policy:
  - min length 6
  - non-alphanumeric required
  - lower/upper case required
  - digit required
  - unique email required
- JwtBearer auth using RSA public key.
- Typed HttpClient for Core role lookup (3s timeout).
- Message bus singleton.

On startup, PrepDb:
1. Applies EF migrations.
2. Ensures Identity roles Student and Teacher exist.

---

## Configuration Keys Used

Production/appsettings.json and Development/appsettings.Development.json include:
- ConnectionStrings:AccountConnectionString
- RsaKeys:PrivateKey
- RsaKeys:PublicKey
- OpenId:Issuer
- CoreServiceUrl
- RabbitMQHost
- RabbitMQPort

---

## DI Lifetimes

| Service | Lifetime |
|---------|----------|
| IUsersRepository | Transient |
| IAuthService | Transient |
| ICoreRoleClient | Typed HttpClient |
| IRsaKeyService | Singleton |
| IMessageBusClient | Singleton |

---

## Tests

File: Authentication.Tests/AuthenticationControllerTests.cs

Covered:
- CanUserRegister() happy path
- CanUserLogin() happy path

Test strategy:
- Mocked: IUsersRepository, IMessageBusClient, ICoreRoleClient, IRsaKeyService
- Real: AuthService, AutoMapper profile

Not yet covered:
- refresh-token endpoint behavior
- .well-known endpoints
- negative/validation/error paths
- RabbitMQ failure behavior
