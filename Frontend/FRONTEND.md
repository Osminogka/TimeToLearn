# Frontend Application

Vue 3 SPA built with Vite. The frontend mirrors backend microservice boundaries and keeps cross-domain concerns in Shared.

---

## Architectural Intent (Required)

The frontend must stay easy to read, understand, maintain, and extend. To enforce that:

1. Domain code stays inside domain folders (Authentication, Core, Courses, Forums, Users).
2. Cross-domain concerns go to Shared (styles, router, common API client, auth state, reusable components).
3. Shared CSS must never be owned by a feature folder.
4. API calls must be split by domain service files and use one shared native fetch client.
5. `/api` proxy target remains `http://acme.com` in development.

---

## Project Structure

```
Frontend/
├── index.html
├── vite.config.js
├── jsconfig.json
├── package.json
└── src/
    ├── main.js
    ├── App.vue
    ├── Authentication/
    │   ├── pages/
    │   │   ├── LoginPage.vue
    │   │   └── RegisterPage.vue
    │   └── services/
    │       └── authApi.js
    ├── Core/
    │   ├── universities/
    │   │   ├── pages/
    │   │   │   └── DashboardUniversties.vue
    │   │   └── services/
    │   │       └── universityApi.js
    │   └── roles/
    │       ├── pages/
    │       │   └── DefineRole.vue
    │       └── services/
    │           └── roleApi.js
    ├── Courses/
    ├── Forums/
    ├── Users/
    └── Shared/
        ├── api/
        │   ├── httpClient.js
        │   └── serviceBasePaths.js
        ├── components/
        │   └── InfoCardGrid.vue
        ├── pages/
        │   └── GreetingPage.vue
        ├── router/
        │   └── index.js
        ├── services/
        │   └── utils.js
        └── styles/
            ├── index.css
            ├── tokens.css
            ├── base.css
            ├── utilities.css
            ├── animations.css
            └── components/
                └── forms.css
```

---

## CSS Ownership Rules

Global styles are imported once in `src/main.js` via `src/Shared/styles/index.css`.

### Shared style layers

- `tokens.css`: design variables (colors, fonts).
- `base.css`: global element-level defaults.
- `utilities.css`: reusable classes (`.default-text`, `.gradient-title`).
- `components/forms.css`: reusable form classes (`.input-field`, `.submit-button`, `.login-form`).
- `animations.css`: reusable transitions/animation classes.

### Feature style rules

- Feature components may keep local layout styles in scoped blocks.
- Feature components may consume shared utility classes.
- Feature folders must not expose CSS intended for other domains.

---

## API Architecture

All HTTP is native fetch through `src/Shared/api/httpClient.js`.

### Shared API files

- `httpClient.js`: standardized GET/POST + error handling.
- `serviceBasePaths.js`: canonical base paths by service.

```js
export const SERVICE_BASE_PATHS = {
  auth: '/api/a/authentication',
  users: '/api/u',
  courses: '/api/c',
  forums: '/api/f',
};
```

### Domain API files

- Authentication APIs: `src/Authentication/services/authApi.js`
- Core role APIs: `src/Core/roles/services/roleApi.js`
- University APIs: `src/Core/universities/services/universityApi.js` (placeholder for upcoming endpoints)
- Courses APIs: `src/Courses/services/courseApi.js` (courses, lessons, progress, grading, lesson mini quizzes, course quizzes)

### Dev proxy

Vite proxy remains:

- `/api` -> `http://acme.com`
- `changeOrigin: true`
- `secure: false`

---

## Routing and Auth

Router file: `src/Shared/router/index.js`

Current route behavior:

- `/` route name: `Main`
- Child routes: `/login` and `/register`
- Login/Register are marked `guestOnly`
- Authenticated users hitting guest-only routes are redirected to `Main`
- Document title is set from route meta

JWT/auth state utility: `src/Shared/services/utils.js`

- Maintains reactive `user` object
- Clears user state when token is missing/invalid/removed

---

## Quiz Experience (Courses Domain)

Quiz features are implemented in the existing course lessons workspace page:

- `src/Core/universities/pages/UniversityCourseLessonsPage.vue`

### Supported quiz flows

1. **Lesson mini quizzes**
- Each lesson can optionally contain teacher-authored single-choice quiz questions.
- Students can submit/update one answer per question.

2. **Course-level quiz mode**
- A course can optionally use quiz questions directly (without lesson content requirements).
- Students answer the same single-choice format as lesson mini quizzes.

### Teacher UI responsibilities

- Create lesson mini quiz questions (question + options + one correct option).
- Create course-level quiz questions.
- Review submitted answers for:
  - selected lesson mini quiz
  - course-level quiz

### Student UI responsibilities

- Answer lesson mini quiz questions.
- Answer course-level quiz questions.
- View only their own submitted answers and correctness-based score summaries.

### Design-system constraints followed

- Uses existing shared classes/tokens (`surface-card`, `submit-button`, `secondary-button`, `input-field`, pills).
- Light theme and purple/pink palette preserved.
- Subtle transitions only; no separate visual subsystem introduced.

---

## Changes Applied in This Refactor

1. Moved all shared styling responsibility into `src/Shared/styles`.
2. Removed shared-style dependency on Authentication folder.
3. Added shared native-fetch client and service base path map.
4. Split auth and role API responsibilities into correct domain service files.
5. Fixed known bugs:
   - Register submit label now says Register.
   - Register redirect now goes to existing route `Main`.
   - Guest redirect no longer points to non-existing Dashboard route.
   - `DefineRole` no longer hardcodes user name.
6. Removed unused code/files:
   - `src/Shared/pages/MainPage.vue`
   - old Authentication CSS files that were acting as global style source
   - old `src/Authentication/services/api.js`

---

## Next Development Rules

1. When adding new shared classes, add them under `src/Shared/styles` only.
2. When adding new endpoint calls, place them in the owning domain service file.
3. Use `SERVICE_BASE_PATHS` constants; do not hardcode service prefixes in random files.
4. Keep feature folders independent from each other; communicate through Shared only.
5. Prefer small reusable Vue components over large page-level style duplication.
