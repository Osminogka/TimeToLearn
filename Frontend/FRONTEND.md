# Frontend Application

Vue 3 SPA built with Vite. Handles authentication, routing, and basic user profile setup. University, courses, and forum features are not yet implemented.

---

## Project Structure

```
Frontend/
├── index.html                        # Mount point (#app), dark background, entry script
├── vite.config.js                    # Dev server port 3000, @/ alias, /api proxy
├── jsconfig.json                     # @/* → ./src/* path alias
├── .eslintrc.cjs                     # vue3-essential + eslint:recommended
├── package.json                      # Scripts and dependencies
├── public/
│   └── favicon.svg
└── src/
    ├── main.js                       # createApp → use(router) → mount('#app')
    ├── App.vue                       # Root layout: single <RouterView />
    ├── Authentication/
    │   ├── pages/
    │   │   ├── LoginPage.vue         # Login form with client-side validation
    │   │   └── RegisterPage.vue      # Register form with client-side validation
    │   ├── services/
    │   │   └── api.js                # POST /login, POST /register, GET /general/:email
    │   └── assets/css/
    │       ├── login-register.css    # Form layout, input and button styles
    │       ├── text-classes.css      # .gradient-title, .default-text
    │       └── transitions.css       # .slide-up-down, .slide-left-right
    ├── Core/
    │   ├── universities/
    │   │   ├── pages/
    │   │   │   └── DashboardUniversties.vue   # Authenticated landing; logout button
    │   │   └── services/
    │   │       └── universityApi.js            # Placeholder (empty)
    │   └── roles/
    │       ├── pages/
    │       │   └── DefineRole.vue              # Profile setup form (UI only, no API)
    │       └── services/
    │           └── roleApi.js                  # Placeholder (empty)
    └── Shared/
        ├── components/
        │   └── InfoCardGrid.vue      # 4 platform info cards shown on landing page
        ├── pages/
        │   ├── GreetingPage.vue      # Unauthenticated landing; embeds RouterView for auth forms
        │   └── MainPage.vue          # Unused
        ├── router/
        │   └── index.js              # All routes + navigation guards
        └── services/
            └── utils.js              # JWT storage, decoding, auth state
```

---

## Routing

**File:** `src/Shared/router/index.js`

| Route | Name | Component | Auth |
|-------|------|-----------|------|
| `/` | Main | `DashboardUniversties` (auth) or `GreetingPage` (guest) | No |
| `/login` | Login | `LoginPage` (lazy) | No |
| `/register` | Register | `RegisterPage` (lazy) | No |
| `/define-role` | DefineRole | `DefineRole` | No |

`/login` and `/register` are **children of `/`** — they render inside `GreetingPage`'s `<RouterView>` with a slide-fade transition.

### Navigation Guards (in order)

1. Redirect authenticated users away from `guestOnly` routes
2. Redirect unauthenticated users away from `requiresAuth` routes → `/login`
3. Set `document.title` from `meta.title` (default: `'Time to Learn'`)

---

## Authentication Flow

### JWT Lifecycle

All JWT logic lives in `src/Shared/services/utils.js`:

```js
saveToken(token)       // localStorage.setItem('jwt', token)
getToken()             // localStorage.getItem('jwt')
clearToken()           // localStorage.removeItem('jwt')
isAuthenticated()      // decodes token, returns true if user.name !== ''
getCurrentUser()       // decodes JWT payload via atob(), populates reactive `user` ref
                       // extracts `unique_name` (→ user.name) and `email`
```

`user` is a Vue `ref` — components import it for reactive access to the current user's name and email.

### Login

```
LoginPage → authApi.login(email, password)
    → POST /api/a/authentication/login
    ← { success, message: "<JWT>" }
    → saveToken(message)
    → router.push('/')   ← shows DashboardUniversties
```

### Register

```
RegisterPage → authApi.register(name, email, password)
    → POST /api/a/authentication/register
    ← { success, message: "<JWT>" }
    → saveToken(message)
    → router.push({ name: 'Dashboard' })   ← ⚠ route name doesn't exist (bug)
```

### Logout

```
DashboardUniversties → authUtils.clearToken() → window.location.reload()
    → isAuthenticated() returns false → GreetingPage shown
```

---

## API Layer

**File:** `src/Authentication/services/api.js`

Base path: `/api/a/authentication` (proxied via Vite to `http://acme.com`)

| Function | Method | Endpoint | Notes |
|----------|--------|----------|-------|
| `login(email, password)` | POST | `/api/a/authentication/login` | Calls `saveToken` on success |
| `register(name, email, password)` | POST | `/api/a/authentication/register` | Calls `saveToken` on success |
| `getRole(email)` | GET | `/api/u/general/{email}` | Returns `{ isTeacher, isStudent }` |

All POSTs go through a shared `postRequest(endpoint, data)` helper that sets `Content-Type: application/json` and returns `{ success, message }`.

### Vite Proxy

`/api` → `http://acme.com` with `changeOrigin: true`, `secure: false`. All backend services are reachable through this single proxy using their path prefixes (`/api/a`, `/api/u`, `/api/c`, `/api/f`).

---

## Client-Side Validation

Both forms validate before calling the API:

| Rule | Login | Register |
|------|-------|----------|
| Fields not empty | Yes | Yes |
| Password ≥ 8 characters | Yes | Yes |
| Email contains `@` and `.` | Yes | Yes |

Errors are shown via a reactive `errorMessage` ref bound in the template.

---

## UI Theme

- **Background:** `#0f0f0f` (page), `#1C1C1C` (cards, header, inputs)
- **Accent:** `#810685` / `#B344C6` (purple) — borders, buttons, gradients
- **Fonts:** Roboto (body), fantasy (gradient titles)
- **Responsive breakpoints:** 768px (grid, header flex-direction), 480px (form input rows)

### CSS Files

| File | Contains |
|------|---------|
| `login-register.css` | Form layout, input dark style, purple submit button |
| `text-classes.css` | `.gradient-title` (purple gradient), `.default-text` |
| `transitions.css` | `.slide-up-down` (bounce, 0.6s), `.slide-left-right` (0.3s) |

---

## Implementation Status

| Feature | Status |
|---------|--------|
| Login / Register | Implemented |
| JWT storage & decoding | Implemented |
| Logout | Implemented |
| Route guards | Implemented |
| `DefineRole` form UI | Implemented — form renders but `submit()` only logs to console; no API call |
| `universityApi.js` | Empty placeholder |
| `roleApi.js` | Empty placeholder |
| University / Courses / Forum pages | Not implemented |

---

## Known Issues

- `RegisterPage` submit button label reads `"Login"` instead of `"Register"`
- After registration, router pushes to `{ name: 'Dashboard' }` which does not exist — navigation silently fails
- `DefineRole.vue` has `user.name` hardcoded to `'Sanzhar'` for testing
