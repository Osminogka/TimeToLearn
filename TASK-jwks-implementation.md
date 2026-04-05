# Task: RSA JWT Signing + JWKS / OpenID Configuration Endpoints

## Overview

Migrate JWT signing from HMAC-SHA256 (symmetric shared secret) to RSA-256 (asymmetric key pair).
The Authentication service holds the private key, signs tokens, and exposes JWKS/OpenID discovery
endpoints. Consumer services (Core, Courses, Forums) validate tokens using the public key fetched
from those endpoints.

---

## Authentication Service Changes (`Backend/Authentication/`)

### 1. RSA Key Service
- Add `IRsaKeyService` / `RsaKeyService` singleton (in `Authentication.API/Infrastructure/` or `Authentication.DL/Services/`)
- Holds hardcoded Base64-encoded RSA-2048 private key and public key (DER format)
- Exposes:
  - `RsaSecurityKey PrivateKey` — for `SigningCredentials`
  - `RsaSecurityKey PublicKey` — for JWKS endpoint
  - `string Kid` — fixed key ID string (e.g. `"ttl-rsa-key-1"`)
  - `JsonWebKey ToJwk()` — returns public key in JWK format (`kty`, `use`, `kid`, `alg`, `n`, `e`)

### 2. JWT Generation (`AuthService.cs`)
- Change signing algorithm from `SecurityAlgorithms.HmacSha256` → `SecurityAlgorithms.RsaSha256`
- Use `IRsaKeyService.PrivateKey` for `SigningCredentials`
- JWT header must include `kid` matching the key service's `Kid`

### 3. JWKS Endpoint — `/.well-known/jwks.json`
- New `WellKnownController` (no `[Authorize]`)
- `GET /.well-known/jwks.json` returns:
  ```json
  {
    "keys": [{
      "kty": "RSA",
      "use": "sig",
      "kid": "ttl-rsa-key-1",
      "alg": "RS256",
      "n": "<base64url modulus>",
      "e": "<base64url exponent>"
    }]
  }
  ```

### 4. OpenID Configuration Endpoint — `/.well-known/openid-configuration`
- On same `WellKnownController`
- `GET /.well-known/openid-configuration` returns minimal discovery document:
  ```json
  {
    "issuer": "<OpenId:Issuer from config>",
    "jwks_uri": "<OpenId:Issuer>/.well-known/jwks.json",
    "token_endpoint": "<OpenId:Issuer>/api/a/authentication/login",
    "id_token_signing_alg_values_supported": ["RS256"],
    "response_types_supported": ["token"]
  }
  ```
- Base URL (`issuer`) read from `OpenId:Issuer` in config

### 5. Configuration (`appsettings.json` / `appsettings.Development.json`)
- Remove `Jwt:Key` (symmetric secret) — no longer used
- Add `RsaKeys:PublicKey` (Base64 DER) and `RsaKeys:PrivateKey` (Base64 DER)
- Add `OpenId:Issuer` (e.g. `http://auth-clusterip-srv` / `http://localhost:5000`)

### 6. `Program.cs`
- Register `IRsaKeyService` as Singleton
- Update JWT Bearer validation on the Authentication service itself (if used) to RS256

---

## Consumer Service Changes (Core, Courses, Forums)

### Strategy: Dynamic JWKS Fetching via `MetadataAddress`

Use ASP.NET Core's built-in support: configure `AddJwtBearer` with `MetadataAddress` pointing to
`/.well-known/openid-configuration`. The middleware auto-fetches and caches the JWKS, and
re-fetches on key rotation (cache TTL ~24h by default).

### `Program.cs` change (same pattern for all three services)
```csharp
// Before
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"])),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

// After
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.MetadataAddress = config["OpenId:MetadataAddress"]; // /.well-known/openid-configuration URL
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = false,   // keep disabled for now
            ValidateAudience = false
        };
        options.RequireHttpsMetadata = false; // dev/k8s internal HTTP
    });
```

### Configuration changes (Core, Courses, Forums)
- Remove `Jwt:Key` from `appsettings.json` and `appsettings.Development.json`
- Add `OpenId:MetadataAddress`:
  - Production: `http://auth-clusterip-srv/.well-known/openid-configuration`
  - Development: `http://localhost:5000/.well-known/openid-configuration` (or whatever port Auth runs on)

---

## Files to Create / Modify

| File | Action |
|------|--------|
| `Authentication.API/Infrastructure/RsaKeyService.cs` | Create |
| `Authentication.API/Controllers/WellKnownController.cs` | Create |
| `Authentication.API/Program.cs` | Update — register RsaKeyService |
| `Authentication.DL/Services/AuthService.cs` | Update — RS256 signing |
| `Authentication.API/appsettings.json` | Update |
| `Authentication.API/appsettings.Development.json` | Update |
| `Core.API/Program.cs` | Update — MetadataAddress JWT config |
| `Core.API/appsettings.json` | Update |
| `Core.API/appsettings.Development.json` | Update |
| `Courses.API/Program.cs` | Update — MetadataAddress JWT config |
| `Courses.API/appsettings.json` | Update |
| `Courses.API/appsettings.Development.json` | Update |
| `Forums.API/Program.cs` | Update — MetadataAddress JWT config |
| `Forums.API/appsettings.json` | Update |
| `Forums.API/appsettings.Development.json` | Update |

---

## Confirmed Decisions

- **JWKS strategy:** Dynamic `MetadataAddress` fetching (ASP.NET Core auto-caches JWKS)
- **Key storage:** `appsettings.json` under `RsaKeys:PrivateKey` / `RsaKeys:PublicKey` (Base64 DER)
- **Dev fallback:** When `RsaKeys` is empty, `RsaKeyService` generates an ephemeral key pair at startup and logs the values so they can be pasted into config
- **Issuer validation:** Enabled — `ValidateIssuer = true`, `ValidIssuer` from `OpenId:Issuer` config
- **Consumer fallback:** Consumer services also accept `OpenId:PublicKey` (Base64); used as `IssuerSigningKey` when MetadataAddress is unreachable

## Status: IMPLEMENTED
