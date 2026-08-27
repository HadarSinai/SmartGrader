# SmartGrader – Frontend Rules (client/)

## Angular Patterns

- All components use `standalone: true` with explicit `imports: [...]` — no `NgModule`.
- Use PrimeNG for all UI: `TableModule`, `ButtonModule`, `CardModule`, `DialogModule`, `InputTextModule`, `CalendarModule`, `TagModule`, `ConfirmDialogModule`, `SkeletonModule`, `TooltipModule`.
- Global toast via `MessageService` (provided in `appConfig`). Import `ToastModule` in root only.

---

## Models (TypeScript Interfaces)

- All models are `interface` types, never `class`.
- Match backend DTO names exactly.
- Use `string | null` for nullable strings, never `string | undefined`.
- Dates are ISO strings (`string`), not `Date` objects.

```typescript
// Correct model pattern
export interface LessonResponseDto {
  id: number;
  name: string | null;
  lessonDate: string; // ISO 8601
  createdAt: string;
  assignmentsCount: number;
}

export interface CreateLessonRequestDto {
  name: string | null;
  subject: string | null;
  lessonDate: string;
  teacherName: string | null;
}
```

---

## HTTP Services

- Every entity has its own service in `src/app/services/`.
- Services inject `ApiClient` (not `HttpClient` directly).
- Use `this.api.url('/api/path')` — never hardcode base URLs.
- All methods return `Observable<T>`, never `Promise`.

```typescript
@Injectable({ providedIn: "root" })
export class LessonsService {
  constructor(private api: ApiClient) {}

  getAll(): Observable<LessonResponseDto[]> {
    return this.api.http.get<LessonResponseDto[]>(this.api.url("/api/lessons"));
  }

  create(dto: CreateLessonRequestDto): Observable<LessonResponseDto> {
    return this.api.http.post<LessonResponseDto>(
      this.api.url("/api/lessons"),
      dto,
    );
  }
}
```

---

## Components

- **Three files per component**, always: `x.component.ts` · `x.component.html` · `x.component.css`.
  The decorator declares `templateUrl: "./x.component.html"` and `styleUrls: ["./x.component.css"]`.
  A component with no styles of its own declares nothing — not an empty `styles: []`.
  Enforced by `ComponentFileLayoutTests`.
- **A style rule has one home.** If two components need the same rule, it belongs in
  `client/src/styles.css`, not copied. A component that must differ adds a **modifier** class; it never
  redefines a base class the global sheet already defines. Enforced by `DesignTokenTests`.
- **No literal colours.** No `#hex`, no `rgb()/rgba()/hsl()`, and no `var(--token, fallback)` — the
  fallback is what hides a misspelled token name for months. Tokens are listed in
  [docs/design-system.md](../docs/design-system.md). Enforced, ratcheted at 0.
- **Status presentation comes from `STATUS_PRESENTATION`** in `models/submission.model.ts` — never a
  local `switch` on the status string. Five copies of that mapping already contradicted each other.
- Inject `MessageService` (from PrimeNG) for all user-facing notifications.
- Use `severity: 'success' | 'error' | 'warn' | 'info'` for toasts.
- Always handle observable errors in `.subscribe({ error: (err) => ... })`.
- Use `ConfirmationService` (PrimeNG) for delete confirmations — never `window.confirm`.
- Navigation via `Router.navigate([...])`, never `location.href`.

---

## Routing Convention

Nested resources follow this URL pattern:

```
/lessons
/lessons/new
/lessons/:id/edit
/lessons/:lessonId/assignments
/lessons/:lessonId/assignments/new
/lessons/:lessonId/assignments/:id/edit
/students/:studentId/submissions
/students/:studentId/submissions/:submissionId
```

---

## Error Handling

- `ApiErrorInterceptor` catches HTTP errors globally and maps them to messages.
- Components show errors via `MessageService`, not `console.error` or `alert`.

---

## TypeScript

- Strict mode enabled — no implicit `any`.
- Always type observables: `Observable<T>`, never `Observable<any>`.
- Use `readonly` for component inputs where applicable.
