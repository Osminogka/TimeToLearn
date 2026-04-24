# Frontend Application

This frontend is a Vue 3 SPA (Vite) organized as a domain-first, shared-kernel architecture:

- Domain features: Authentication, Core, Courses, Forums, Users.
- Shared kernel: router, API client, auth/session utilities, reusable components, and global design system.

The structure mirrors backend service boundaries while keeping cross-domain concerns centralized in `src/Shared`.

---

## Tech Stack

- Runtime: Vue 3 + Vue Router 4
- Build/dev server: Vite 5
- Markdown pipeline: `marked` + `dompurify`
- Linting: ESLint + `eslint-plugin-vue`

Entry flow:

- `src/main.js` imports `src/Shared/styles/index.css`, creates app, and mounts router.
- `src/App.vue` renders `<RouterView />` and conditionally renders primary navigation for authenticated routes.

---

## Architecture Used

## 1) Domain-Driven Frontend Folders

Each bounded context owns its pages/components/services:

- `src/Authentication`: login/register flows and auth APIs.
- `src/Core`: roles + university workspace (largest feature area).
- `src/Courses`: reusable course/lesson/markdown components + course services.
- `src/Forums`: forum topic/comment components + forum services.
- `src/Users`: account management page + user APIs.

## 2) Shared Kernel (`src/Shared`)

Contains all cross-domain concerns:

- `api/httpClient.js`: centralized fetch wrapper with auth headers + structured `ApiError` handling.
- `api/serviceBasePaths.js`: single source of truth for service prefixes.
- `router/index.js`: all route registration + guards + document title handling.
- `services/utils.js`: JWT storage/decoding, reactive user state, role helpers.
- `services/useBackNavigation.js`: tiny reusable back-navigation composable.
- `components/*`: reusable UI primitives.
- `styles/*`: global tokens/base/utilities/forms/animations.

## 3) Service Layer Pattern

All HTTP calls are routed through one shared client. Feature services (e.g., `authApi`, `universityApi`, `courseApi`, `forumApi`, `userApi`) only compose endpoint paths and payloads.

## 4) Route Meta + Guard Driven Access Control

Navigation uses route meta (`requiresAuth`, `guestOnly`, `requiresTeacher`, `hidePrimaryNavigation`) and centralized guards in router.

---

## Current Folder Structure (Implementation Snapshot)

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
    │   ├── services/
    │   │   ├── authApi.js
    │   │   └── authFormHelpers.js
    │   └── assets/
    ├── Core/
    │   ├── roles/
    │   │   ├── pages/DefineRole.vue
    │   │   └── services/roleApi.js
    │   └── universities/
    │       ├── components/UniversityContextHeader.vue
    │       ├── pages/
    │       │   ├── CreateUniversityPage.vue
    │       │   ├── MyUniversitiesPage.vue
    │       │   ├── UniversitiesCatalogPage.vue
    │       │   ├── UniversityInfoPage.vue
    │       │   ├── UniversityCoursesPage.vue
    │       │   ├── UniversityCourseLessonsPage.vue
    │       │   ├── UniversityCourseLessonEditPage.vue
    │       │   ├── UniversityCourseLessonDetailPage.vue
    │       │   └── UniversityForumsPage.vue
    │       └── services/universityApi.js
    ├── Courses/
    │   ├── components/
    │   │   ├── CourseCard.vue
    │   │   ├── CourseFormPanel.vue
    │   │   ├── LessonCard.vue
    │   │   ├── LessonFormPanel.vue
    │   │   ├── LessonListMobileItem.vue
    │   │   └── MarkdownEditor.vue
    │   └── services/
    │       ├── courseApi.js
    │       ├── lessonResourcesService.js
    │       └── markdownService.js
    ├── Forums/
    │   ├── components/
    │   │   ├── ForumTopicCard.vue
    │   │   └── ForumCommentNode.vue
    │   └── services/forumApi.js
    ├── Users/
    │   ├── pages/AccountManagementPage.vue
    │   ├── components/
    │   └── services/userApi.js
    └── Shared/
        ├── api/
        │   ├── httpClient.js
        │   └── serviceBasePaths.js
        ├── components/
        │   ├── AppIcon.vue
        │   ├── InfoCardGrid.vue
        │   └── PrimaryNavigation.vue
        ├── pages/
        │   └── GreetingPage.vue
        ├── router/
        │   └── index.js
        ├── services/
        │   ├── useBackNavigation.js
        │   └── utils.js
        └── styles/
            ├── index.css
            ├── tokens.css
            ├── base.css
            ├── utilities.css
            ├── animations.css
            └── components/forms.css
```

---

## Page Inventory

Routable pages currently present:

- Shared:
  - `src/Shared/pages/GreetingPage.vue`
- Authentication:
  - `src/Authentication/pages/LoginPage.vue`
  - `src/Authentication/pages/RegisterPage.vue`
- Core Roles:
  - `src/Core/roles/pages/DefineRole.vue`
- Core Universities:
  - `src/Core/universities/pages/UniversitiesCatalogPage.vue`
  - `src/Core/universities/pages/MyUniversitiesPage.vue`
  - `src/Core/universities/pages/CreateUniversityPage.vue`
  - `src/Core/universities/pages/UniversityCoursesPage.vue`
  - `src/Core/universities/pages/UniversityCourseLessonsPage.vue`
  - `src/Core/universities/pages/UniversityCourseLessonEditPage.vue`
  - `src/Core/universities/pages/UniversityCourseLessonDetailPage.vue`
  - `src/Core/universities/pages/UniversityInfoPage.vue`
  - `src/Core/universities/pages/UniversityForumsPage.vue`
- Users:
  - `src/Users/pages/AccountManagementPage.vue`

Notes:

- `src/Courses/pages/` is currently empty; course UX is mounted through Core university workspace pages and reusable course components.
- `src/Forums` currently contributes reusable components/services, with page-level composition happening in `UniversityForumsPage.vue`.

---

## Routing Architecture

Router file: `src/Shared/router/index.js`

### Route design

- Marketing/auth shell:
  - `/` -> `Main` (`GreetingPage`)
  - child routes: `/login`, `/register`
- Role setup:
  - `/define-role`
- University workspace:
  - `/universities`
  - `/universities/my`
  - `/universities/create` (teacher-only)
  - `/universities/:name/courses`
  - `/universities/:name/courses/:courseId/lessons`
  - `/universities/:name/courses/:courseId/lessons/create`
  - `/universities/:name/courses/:courseId/lessons/:lessonId(\d+)/edit`
  - `/universities/:name/courses/:courseId/lessons/:lessonId(\d+)`
  - `/universities/:name/info`
  - `/universities/:name/forums`
- User account:
  - `/account`

### Guards and navigation behavior

- Authenticated users visiting `Main` or any `guestOnly` page are redirected to `UniversitiesMine`.
- `requiresAuth` routes redirect unauthenticated users to `Login`.
- `requiresTeacher` routes redirect non-teachers to `UniversitiesMine`.
- Document title is set from `route.meta.title`.
- `App.vue` hides/shows `PrimaryNavigation` using route meta + auth state.

---

## API Architecture

All network traffic goes through `src/Shared/api/httpClient.js`.

### Shared base paths

```js
export const SERVICE_BASE_PATHS = {
    auth: '/api/a/authentication',
    users: '/api/u',
    courses: '/api/c',
    forums: '/api/f',
};
```

### Shared client capabilities

- `get`, `post` (public)
- `getAuth`, `postAuth`, `putAuth`, `deleteAuth` (JWT-protected)
- Consistent error normalization through `ApiError`
- Friendly status-based fallback messages
- HTML payload detection to avoid leaking raw server pages

### Domain services by responsibility

- `authApi.js`: login/register/refresh token.
- `roleApi.js`: role lookup + teacher/student transitions + teacher verification.
- `universityApi.js`: catalog, membership, invites, university updates, create.
- `courseApi.js`: courses, lessons, lesson ordering/progress, grading, lesson/course quizzes, answer review.
- `forumApi.js`: topics/comments CRUD-lite + reactions.
- `userApi.js`: profile management + invites management.

---

## Auth and Session Model

JWT is persisted in `localStorage` under key `jwt`.

`src/Shared/services/utils.js` provides:

- reactive `user` state (`name`, `email`, `role`)
- JWT payload parsing with base64url decode
- role helpers (`isTeacherRole`, `isDirectorRole`, `isStudentRole`, `canManageUniversityContent`)
- guard-friendly role checks (`hasRole`, `getCurrentRole`)
- token lifecycle (`saveToken`, `clearToken`)

---

## Styling System

Global style entry: `src/Shared/styles/index.css` (imported once in `src/main.js`).

Layered CSS architecture:

- `tokens.css`: design tokens (colors, radii, spacing, fonts, transitions).
- `base.css`: element resets + global background/typography behavior.
- `utilities.css`: reusable utility classes.
- `components/forms.css`: reusable form primitives.
- `animations.css`: shared motion/transitions.

Current visual direction:

- Light-only palette (`color-scheme: light`)
- Purple/pink gradient accents
- Google fonts: Manrope + Space Grotesk

---

## Course/Lesson/Quiz Implementation Notes

Course and lesson editing/display is composed in university workspace pages under `src/Core/universities/pages/*` and backed by `src/Courses/services/*`.

Quiz support currently includes:

- Lesson quizzes and course-level quizzes
- Teacher authoring/upsert/update flows
- Student submission flows
- Per-student answer retrieval + teacher answer review endpoints

Markdown/content safety pipeline:

- `markdownService.js` renders markdown with `marked` and sanitizes output using `DOMPurify`.
- `lessonResourcesService.js` normalizes/embeds lesson resources and supports legacy resource fields.

---

## Dev Server and Proxy

`vite.config.js` currently configures:

- dev host: `0.0.0.0`
- dev port: `3000`
- proxy:
  - `/api` -> `https://timetolearn.ddns.net`
  - `changeOrigin: true`
  - `secure: false`

Important: this differs from the older `acme.com` convention and should be treated as the source of truth unless intentionally changed.

---

## Practical Conventions Going Forward

1. Keep reusable cross-domain code in `src/Shared` only.
2. Keep domain APIs in domain `services` files; avoid direct `fetch` in components/pages.
3. Reuse `SERVICE_BASE_PATHS` constants to prevent path drift.
4. Keep pages focused on orchestration/state; move reusable UI into domain/shared components.
5. Preserve the route-meta driven authorization pattern in router guards.
