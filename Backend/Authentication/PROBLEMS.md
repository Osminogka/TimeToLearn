# Authentication Service — Known Problems

## Security

### 1. JWT secret committed to source control
**Severity:** Critical  
**Location:** `appsettings.json`, `appsettings.Development.json`, `Authentication.Tests/AuthenticationControllerTests.cs:149`

The key `EUt719k5GENP1pWWhrmyDldHPaKXyIa9yImWhPuqHBUlgZ10Fk` is hardcoded in committed files and documentation. Anyone with repo access can forge tokens for any user.

**Fix:** Load from an environment variable or secrets manager. Remove from all committed files. Rotate the key.

---

### 2. User enumeration via login error messages
**Severity:** High  
**Location:** `Authentication.DL/Services/AuthService.cs:30, 40`

`LoginAsync` returns `"User doesn't exist"` for an unknown email and `"Invalid password"` for a wrong password. An attacker can probe which emails are registered.

**Fix:** Return a single generic message — `"Invalid email or password"` — for both failure cases.

---

### 3. No `[EmailAddress]` validation on request models
**Severity:** Medium  
**Location:** `Authentication.DAL/SideModels/LoginRequestModel.cs`, `RegisterRequestModel.cs`

Both models have `[Required]` but no `[EmailAddress]` attribute. Any arbitrary string passes as a valid email.

**Fix:** Add `[EmailAddress]` to `Email` in both models. Also move the length constraint from the service into `[MaxLength(50)]` attributes on the model properties.

---

### 4. `ResponseMessage` carries the `AppUser` Identity entity
**Severity:** Medium  
**Location:** `Authentication.DAL/SideModels/ResponseMessage.cs:7`, `Authentication.DL/Services/AuthService.cs:82`

`ResponseMessage.User` holds the full `AppUser` Identity entity. `AuthService.RegisterAsync` populates it so the controller can map it for RabbitMQ. The controller strips it before responding, but any future code path that serializes `ResponseMessage` directly would leak the Identity object.

**Fix:** Use a dedicated `RegisterResultDto` with only the fields needed (e.g., `Id`, `Email`, `UserName`), or return the created user separately from the HTTP response model.

---

### 5. No rate limiting on login or register
**Severity:** Medium  
**Location:** `Authentication.API/Controllers/AuthenticationController.cs`

Neither endpoint has brute-force protection.

**Fix:** Add ASP.NET Core's built-in rate limiting middleware (available in .NET 7+) on `/login` and `/register`.

---

## Architecture

### 6. RabbitMQ publishing inside the controller
**Severity:** High  
**Location:** `Authentication.API/Controllers/AuthenticationController.cs:61-63`

The controller maps the user and publishes the RabbitMQ event directly. This puts business logic (which event to fire, which payload to send) in the HTTP layer.

**Fix:** Move the publish call into `AuthService.RegisterAsync`. Inject `IMessageBusClient` into the service instead of the controller.

---

### 7. `IUsersRepository` injected into the controller but never used
**Severity:** Low  
**Location:** `Authentication.API/Controllers/AuthenticationController.cs:18, 22`

`_userRepository` is declared and injected but neither endpoint calls it — all DB access goes through `_authService`.

**Fix:** Remove `IUsersRepository` from the controller constructor.

---

### 8. `AuthService` depends on `IConfiguration` directly
**Severity:** Medium  
**Location:** `Authentication.DL/Services/AuthService.cs:17`

The business logic layer reads `Jwt:key` from the raw `IConfiguration`. DL-layer classes should not depend on the configuration system.

**Fix:** Create a `JwtOptions` POCO, bind it in `Program.cs`, and inject `IOptions<JwtOptions>` instead.

---

### 9. `AuthService` imports `Microsoft.AspNetCore.Mvc`
**Severity:** Low  
**Location:** `Authentication.DL/Services/AuthService.cs:4`

An API-layer namespace is imported in a business-logic layer project. The import is unused but represents a wrong dependency direction.

**Fix:** Remove the `using Microsoft.AspNetCore.Mvc;` line.

---

### 10. `TeacherModel.cs` is dead code
**Severity:** Low  
**Location:** `Authentication.DAL/SideModels/TeacherModel.cs`

Nothing references this file. It was left as a placeholder.

**Fix:** Delete it.

---

## Resilience

### 11. RabbitMQ `IModel` (channel) is not thread-safe, but `MessageBusClient` is a Singleton
**Severity:** High  
**Location:** `Authentication.API/AsyncDataService/MessageBusClient.cs:11`

RabbitMQ's `IModel` is explicitly documented as not thread-safe. `MessageBusClient` is registered as a Singleton, so concurrent registrations will race on `_channel.BasicPublish`.

**Fix:** Protect publishes with a `SemaphoreSlim`, or create and dispose a new channel per publish call.

---

### 12. No reconnection logic after RabbitMQ disconnect
**Severity:** Medium  
**Location:** `Authentication.API/AsyncDataService/MessageBusClient.cs:86`

The `ConnectionShutdown` handler only logs. If RabbitMQ restarts after the service starts, all subsequent publishes silently drop.

**Fix:** Implement reconnection in the shutdown handler using a Polly retry policy or a background timer.

---

### 13. `Dispose()` defined but `IDisposable` not implemented
**Severity:** Medium  
**Location:** `Authentication.API/AsyncDataService/MessageBusClient.cs:73`

`MessageBusClient` has a `Dispose()` method but does not implement `IDisposable`. The DI container never calls it, leaking the AMQP connection.

**Fix:** Add `: IDisposable` to both `MessageBusClient` and `IMessageBusClient`.

---

### 14. Extra database round-trip after registration
**Severity:** Low  
**Location:** `Authentication.DL/Services/AuthService.cs:82`

After `CreateAsync` succeeds, `GetByEmailAsync` is called again solely to obtain the user's generated `Id`. `UserManager.CreateAsync` already populates `user.Id` on the object passed in.

**Fix:** Remove the second `GetByEmailAsync` call and use the `user` object already in scope.

---

### 15. Synchronous `Database.Migrate()` called inside an async method
**Severity:** Low  
**Location:** `Authentication.API/Infrastructure/PrepDb.cs:22`

`SeedData` is an `async` method but calls the synchronous `Database.Migrate()`. This can cause thread-pool starvation under some hosting configurations.

**Fix:** Replace with `await context.Database.MigrateAsync()`.

---

## Code Quality

### 16. `getUserEmail()` throws `NullReferenceException` when unauthenticated
**Severity:** Medium  
**Location:** `Authentication.API/Controllers/BaseController.cs:12`

`FirstOrDefault(...)` returns `null` when no `ClaimTypes.Name` claim is present, and `.Value` then throws. The endpoints in this service are not `[Authorize]`, but the helper is a latent crash for any future protected endpoint.

**Fix:** Use null-conditional access: `?.Value`, or guard with an explicit null check.

---

### 17. `HandleException` leaks internal implementation details
**Severity:** Low  
**Location:** `Authentication.API/Controllers/BaseController.cs:20`

The message `"More than one element satisfies the condition in SingleOrDefault."` exposes internal query logic to clients.

**Fix:** Replace all 500-branch messages with a single generic `"An internal server error occurred."`.

---

### 18. `GetUserManager()` breaks repository encapsulation
**Severity:** Medium  
**Location:** `Authentication.DL/Repositories/UserRepository.cs:44`

`GetUserManager()` exposes the raw `UserManager<AppUser>`, allowing callers to bypass the repository entirely.

**Fix:** Add any required operations as explicit interface methods on `IUsersRepository` and remove `GetUserManager()`.

---

### 19. `Console.WriteLine` used instead of `ILogger`
**Severity:** Low  
**Location:** `Authentication.API/AsyncDataService/MessageBusClient.cs`, `Authentication.API/Infrastructure/PrepDb.cs`

`Console.WriteLine` bypasses structured logging, log levels, and configured sinks.

**Fix:** Inject `ILogger<T>` and replace all `Console.WriteLine` calls with `_logger.LogInformation(...)` / `_logger.LogError(...)`.

---

### 20. Tests cover only the happy path
**Severity:** Low  
**Location:** `Authentication.Tests/AuthenticationControllerTests.cs`

Only `CanUserRegister` and `CanUserLogin` exist, both testing a successful outcome. There are no tests for duplicate email, wrong password, empty name/email, inputs exceeding 50 characters, or RabbitMQ publish failure.

**Fix:** Add negative-path tests for each validation branch in `AuthService.RegisterAsync` and `AuthService.LoginAsync`.

---

## Summary

| # | Severity | Category | Location |
|---|----------|----------|----------|
| 1 | Critical | Security | `appsettings.json`, test file |
| 2 | High | Security | `AuthService.cs:30, 40` |
| 3 | Medium | Security | `LoginRequestModel.cs`, `RegisterRequestModel.cs` |
| 4 | Medium | Security | `ResponseMessage.cs`, `AuthService.cs:82` |
| 5 | Medium | Security | `AuthenticationController.cs` |
| 6 | High | Architecture | `AuthenticationController.cs:61-63` |
| 7 | Low | Architecture | `AuthenticationController.cs:18` |
| 8 | Medium | Architecture | `AuthService.cs:17` |
| 9 | Low | Architecture | `AuthService.cs:4` |
| 10 | Low | Architecture | `TeacherModel.cs` |
| 11 | High | Resilience | `MessageBusClient.cs:11` |
| 12 | Medium | Resilience | `MessageBusClient.cs:86` |
| 13 | Medium | Resilience | `MessageBusClient.cs:73` |
| 14 | Low | Resilience | `AuthService.cs:82` |
| 15 | Low | Resilience | `PrepDb.cs:22` |
| 16 | Medium | Quality | `BaseController.cs:12` |
| 17 | Low | Quality | `BaseController.cs:20` |
| 18 | Medium | Quality | `UserRepository.cs:44` |
| 19 | Low | Quality | `MessageBusClient.cs`, `PrepDb.cs` |
| 20 | Low | Quality | `AuthenticationControllerTests.cs` |
