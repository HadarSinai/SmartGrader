# תוכנית מימוש: פעמון התראות

> זו תוכנית שלמה למימוש פיצ'ר התראות (Notifications Bell) בפעמון ב-topbar, לפי האפיון ב-master-spec §7.
> משדה יום: 2026-07-14 | ארכיטקטורה: Clean Architecture + CQRS (שרת), standalone components + signals (קליינט)
> **עודכן 2026-07-19**: נבדק מול הקוד בפועל לפני מימוש — 9 סטיות מהמוסכמות הקיימות תוקנו (ראה סימוני "**תוקן**" בהמשך): שם שדה ה-repository (`_context`), שם קובץ ה-handler, roles ב-`[Authorize]`, שימור לוגו/ניווט-Admin בטופבר, טוקני CSS אמיתיים, פורמט תאריך אחיד, flags לפקודת המיגרציה, וקריאה שגויה ש-`getRecent()` לא בשימוש (בפועל `dashboard.component.ts` תלוי בו — **אין** למחוק).

---

## מה קיים היום (הבסיס)

- **שרת**: `AiWorker` ([AiWorker.cs](../../server/Api/BackgroundServices/AiWorker.cs)) מסמן הגשה כ-`Done` דרך `MarkDone(...)` ב-[Submission.cs](../../server/Domain/Entities/Submission.cs). לישות יש `SubmittedAt` אבל **אין timestamp לרגע סיום הבדיקה**.
- **קליינט**: הפעמון ב-[topbar.component.ts](../../client/src/app/components/layout/topbar.component.ts#L41-L50) הוא placeholder בלבד. ב-[submissions.service.ts](../../client/src/app/services/submissions.service.ts#L69-L74) יש כבר `getRecent()` stub שמצביע על `/api/students/submissions/recent` — endpoint שלא קיים בשרת.
- ההתראות מיועדות **למורה בלבד** (הניווט ב-topbar עטוף ב-`auth.isTeacher()`).

---

## החלטת מפתח: איך יודעים "מה חדש"

צריך להחליט מה מגדיר "התראה שלא נקראה". שתי גישות:

| גישה                                    | יתרון                  | חיסרון                           |
| --------------------------------------- | ---------------------- | -------------------------------- |
| **A – client lastSeen** (מומלץ ל-MVP)   | אפס שינויים ב-DB, מהיר | ה"נקרא" לא מסונכרן בין מכשירים   |
| **B – שדה `GradedAt` + מעקב read בשרת** | מדויק, חוצה-מכשירים    | דורש migration + טבלת read-state |

**בחירה**: התוכנית משתמשת ב-**גישה A** עם הערה איפה משדרגים ל-B בעתיד.

---

## Phase 1 — שרת: endpoint להגשות שנבדקו לאחרונה

### 1.1 (אופציונלי, מומלץ) להוסיף `GradedAt` לישות

**קובץ**: `server/Domain/Entities/Submission.cs`

הוסף שדה:

```csharp
public DateTime? GradedAt { get; private set; }
```

עדכן `MarkDone` להגדיר `GradedAt = DateTime.UtcNow;`

דורש EF migration. בלי זה ניתן להשתמש ב-`SubmittedAt` לצורך מיון (פחות מדויק).

### 1.2 Repository — שאילתה חוצת-תלמידים

**Skill**: `backend-repository-query-pattern` | **Agent**: `phase-repository-implementation` (subagent מצומצם-scope שמוסיף רק חתימת מתודה + מימוש EF Core; לא נוגע ב-handlers/controllers)

**קובץ**: `server/Domain/Abstractions/ISubmissionRepository.cs`

הוסף חתימת מתודה:

```csharp
Task<IReadOnlyList<Submission>> GetRecentGradedAsync(int limit, CancellationToken ct);
```

**קובץ**: `server/Infrastructure/Repositories/SubmissionRepository.cs`

מימוש (**תוקן**: שם השדה בפועל הוא `_context`, לא `_db` — ראה שאר המתודות באותו קובץ):

```csharp
public async Task<IReadOnlyList<Submission>> GetRecentGradedAsync(int limit, CancellationToken ct = default) =>
    await _context.Submissions
        .Where(s => s.Status == SubmissionStatus.Done)
        .Include(s => s.Student)
        .Include(s => s.Assignment)
        .OrderByDescending(s => s.GradedAt ?? s.SubmittedAt)
        .Take(limit)
        .AsNoTracking()
        .ToListAsync(ct);
```

### 1.3 CQRS Query + Handler

**Skill**: `backend-mediatr-query-handler-pattern` | **Agent**: `phase-query-handler-implementation` (subagent מצומצם-scope; מניח שהמתודה ב-repository כבר קיימת מ-1.2 — לא יוצר אותה בעצמו)

**תיקיה חדשה**: `server/Application/UseCases/Notifications/GetRecentGradedSubmissions/`

**קובץ**: `GetRecentGradedSubmissionsQuery.cs`

```csharp
using MediatR;
using SmartGrader.Application.Dtos.Submissions;

namespace SmartGrader.Application.UseCases.Notifications.GetRecentGradedSubmissions;

public record GetRecentGradedSubmissionsQuery(int Limit = 20)
    : IRequest<IReadOnlyList<SubmissionResponseDto>>;
```

**קובץ**: `GetRecentGradedSubmissionsHandler.cs` (**תוקן**: כל ה-handlers הקיימים בקוד נקראים `{Verb}Handler.cs`, לא `{Verb}QueryHandler.cs` — ראה `GetSubmissionsHandler.cs`, `GetSubmissionByIdHandler.cs`)

```csharp
using AutoMapper;
using MediatR;
using SmartGrader.Application.Dtos.Submissions;
using SmartGrader.Domain.Abstractions;

namespace SmartGrader.Application.UseCases.Notifications.GetRecentGradedSubmissions;

public class GetRecentGradedSubmissionsHandler
    : IRequestHandler<GetRecentGradedSubmissionsQuery, IReadOnlyList<SubmissionResponseDto>>
{
    private readonly ISubmissionRepository _repo;
    private readonly IMapper _mapper;

    public GetRecentGradedSubmissionsHandler(ISubmissionRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<SubmissionResponseDto>> Handle(
        GetRecentGradedSubmissionsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _repo.GetRecentGradedAsync(request.Limit, cancellationToken);
        return _mapper.Map<IReadOnlyList<SubmissionResponseDto>>(items);
    }
}
```

> משתמש ב-`SubmissionResponseDto` הקיים; אין צורך ב-DTO חדש.

### 1.4 Controller endpoint

**Skill**: `backend-controller-endpoint-pattern` — אין subagent ייעודי לשלב זה (בניגוד ל-1.2/1.3); לבצע ישירות לפי הסקיל.

**קובץ חדש**: `server/Api/Controllers/NotificationsController.cs`

```csharp
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGrader.Application.Dtos.Submissions;
using SmartGrader.Application.UseCases.Notifications.GetRecentGradedSubmissions;

namespace SmartGrader.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Teacher,Admin")] // תוקן: כל controller מורה-פונה בקוד משתמש ב-"Teacher,Admin", לא "Teacher" בלבד
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get recently graded submissions (last N days).
    /// Teachers only.
    /// </summary>
    [HttpGet("graded-submissions")]
    public async Task<IActionResult> GetGraded(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetRecentGradedSubmissionsQuery(limit),
            cancellationToken);
        return Ok(result);
    }
}
```

> הערה: ה-stub הקיים בקליינט מצביע על `/api/students/submissions/recent`. עדיף להשאיר route ייעודי להתראות ולתקן את הקליינט בהתאם.

---

## Phase 2 — קליינט: שירות התראות + UI

### 2.1 שירות `NotificationsService` עם polling + signals

אין skill/agent ייעודי לשירות HTTP חדש בקליינט — עוקב אחרי הדפוס הכללי המתועד ב-[client/CLAUDE.md](../../client/CLAUDE.md) (`ApiClient`, `Observable<T>`, `providedIn: 'root'`); לבצע ישירות.

**קובץ חדש**: `client/src/app/services/notifications.service.ts`

```typescript
import { Injectable, signal, computed } from "@angular/core";
import { timer } from "rxjs";
import { switchMap, catchError } from "rxjs/operators";
import { of } from "rxjs";
import { SubmissionResponseDto } from "@models/submission.model";
import { ApiClient } from "../core/http/api-client";
import { AuthService } from "./auth.service";

@Injectable({ providedIn: "root" })
export class NotificationsService {
  private readonly _items = signal<SubmissionResponseDto[]>([]);
  readonly items = this._items.asReadonly();

  // גישה A: lastSeen מ-localStorage (MVP)
  private readonly lastSeenId = signal<number>(
    Number(localStorage.getItem("notif.lastSeenId") ?? 0),
  );
  readonly unreadCount = computed(
    () => this._items().filter((s) => s.id > this.lastSeenId()).length,
  );

  private polling$ = timer(0, 30_000);
  private isStarted = false;

  constructor(
    private api: ApiClient,
    private auth: AuthService,
  ) {}

  /**
   * Start polling for graded submissions.
   * Call this once on app initialization (e.g., in app.component.ts ngOnInit or in the Shell).
   * Only works for teachers.
   */
  start(): void {
    if (this.isStarted || !this.auth.isTeacher()) return;
    this.isStarted = true;

    this.polling$
      .pipe(
        switchMap(() =>
          this.api.http.get<SubmissionResponseDto[]>(
            this.api.url("/api/notifications/graded-submissions?limit=20"),
          ),
        ),
        catchError(() => of([])), // שכשל שקט — בלי טוסט
      )
      .subscribe((list) => this._items.set(list));
  }

  /**
   * Mark all notifications as read.
   * Typically called when opening the notification panel.
   */
  markAllRead(): void {
    const maxId = Math.max(0, ...this._items().map((s) => s.id));
    this.lastSeenId.set(maxId);
    localStorage.setItem("notif.lastSeenId", String(maxId));
  }

  /**
   * Manually refresh the list (e.g., from a button click).
   * Note: polling continues in the background.
   */
  refresh(): void {
    // ניתן לקרוא ידנית ל-trigger ריפרש (אופציונלי)
    // TODO: implement manual trigger if needed
  }
}
```

### 2.2 עדכון topbar component (**תוקן**: עריכה ממוקדת, לא רה-כתיבה מלאה)

**קובץ**: `client/src/app/components/layout/topbar.component.ts`

**Skill**: `client-design-token-rollout-pattern` (לבלוק ה-`styles` בשלב 6 — שימוש ב-`sg-*` classes וטוקנים קיימים בלבד, בלי צבעים/מחלקות אד-הוק חדשים). **אין** agent ייעודי לעריכת הטופבר עצמה — `phase-client-flow-fix-implementation` מוגבל ל-`[Fix]` items מתוך `docs/ux/{feature}-flow.md` קיים (Lessons/Students/Assignments/Submissions בלבד), ואין flow doc כזה לפעמון; לבצע ישירות.

⚠️ **הערה קריטית**: הקובץ האמיתי היום (שורות 1-139) **כבר** מכיל לוגו `sg-brand`, ניווט מלא ל-`auth.isTeacher() || auth.isAdmin()` כולל לינק "יומן מערכת" (`auth.isAdmin()` בלבד), ו-avatar+logout. **אין** להחליף את כל ה-template — זה ימחק את הלוגו ואת הניווט ל-Admin בטעות. יש לגעת **רק** בבלוק כפתור הפעמון הקיים (שורות 71-80 בקובץ הנוכחי):

```typescript
<p-button
  icon="pi pi-bell"
  [text]="true"
  [rounded]="true"
  severity="secondary"
  ariaLabel="התראות"
  pTooltip="התראות יהיו זמינות בקרוב"
  tooltipPosition="bottom"
>
</p-button>
```

**שלבי העריכה**:

1. **imports** — הוסף `BadgeModule` (`primeng/badge`), `OverlayPanelModule`/`OverlayPanel` (`primeng/overlaypanel` — תואם ל-PrimeNG 17.18.15 המותקנת; טרם השתנה ל-`Popover`), ו-`NotificationsService` (`../../services/notifications.service`). `CommonModule` לא נדרש — הקובץ כבר משתמש ב-`@if`/`@for` (control flow syntax).
2. **constructor** — הוסף `public notifications: NotificationsService` לצד `auth` ו-`router` הקיימים.
3. **`ngOnInit`** — הקומפוננטה כיום לא `implements OnInit`; הוסף זאת, עם `ngOnInit(): void { this.notifications.start(); }`.
4. **`@ViewChild`** — הוסף `@ViewChild("notifPanel") notifPanel!: OverlayPanel;` ומתודה `toggleNotifications(event: Event): void { this.notifPanel?.toggle(event); }`.
5. **בתוך ה-template**, החלף את בלוק כפתור הפעמון שלמעלה (בתוך אותו `<div class="p-toolbar-group-left flex align-items-center gap-2">` — לפני בלוק ה-avatar/logout, **שנשאר ללא שינוי**) ב:
   ```html
   <p-button
     icon="pi pi-bell"
     [text]="true"
     [rounded]="true"
     severity="secondary"
     ariaLabel="התראות"
     [badge]="
       notifications.unreadCount() > 0
         ? notifications.unreadCount().toString()
         : undefined
     "
     badgeClass="p-badge-danger"
     (onClick)="toggleNotifications($event)"
   ></p-button>

   <p-overlayPanel
     #notifPanel
     (onShow)="notifications.markAllRead()"
     styleClass="sg-notif-panel"
   >
     @if (notifications.items().length === 0) {
       <div class="sg-notif-empty">אין התראות חדשות</div>
     } @else {
       <ul class="sg-notif-list" role="list">
         @for (n of notifications.items(); track n.id) {
           <li class="sg-notif-item">
             <a
               [routerLink]="['/students', n.studentId, 'submissions', n.id]"
               (click)="notifPanel.hide()"
             >
               <span class="sg-notif-text">
                 ההגשה של {{ n.studentName }} בתרגיל "{{ n.assignmentName }}" נבדקה
               </span>
               <span class="sg-notif-time">
                 {{ n.submittedAt | date: "dd.MM.yy HH:mm" }}
               </span>
             </a>
           </li>
         }
       </ul>
     }
   </p-overlayPanel>
   ```
   (**תוקן**: פורמט התאריך `dd.MM.yy HH:mm` — הפורמט האחיד בכל הקוד הקיים, למשל `submissions-list.component.html:163`, `dashboard.component.ts:121` — לא `dd/MM/yyyy HH:mm`.)
6. **`styles`** — הוסף ל-`styles` array הקיים (אל תחליף את `.sg-topbar-user` הקיים), **בטוקנים האמיתיים** מ-`client/src/styles.css` (**תוקן**: הטוקנים `--space-md`, `--space-xs`, `--text-xs`, `--app-text-weak`, `--app-bg-hover` **לא קיימים** בפרויקט — הסקאלה האמיתית היא `--space-1..4,6`, `--text-sm/base/lg/xl`, `--app-muted`):
   ```css
   .sg-notif-empty {
     padding: var(--space-4);
     text-align: center;
     color: var(--app-muted);
     font-size: var(--text-sm);
   }

   .sg-notif-list {
     list-style: none;
     margin: 0;
     padding: 0;
     min-width: 320px;
     max-width: 480px;
     max-height: 400px;
     overflow-y: auto;
   }

   .sg-notif-item {
     border-bottom: 1px solid var(--app-border);
   }

   .sg-notif-item:last-child {
     border-bottom: none;
   }

   .sg-notif-item a {
     display: flex;
     flex-direction: column;
     gap: var(--space-1);
     padding: var(--space-3);
     text-decoration: none;
     color: var(--app-text);
     transition: background-color 0.2s;
   }

   .sg-notif-item a:hover {
     background-color: var(--app-surface-2);
   }

   .sg-notif-text {
     font-weight: 500;
     line-height: 1.4;
   }

   .sg-notif-time {
     font-size: var(--text-sm);
     color: var(--app-muted);
   }

   :global(.sg-notif-panel .p-overlaypanel-content) {
     padding: 0;
     border-radius: var(--radius-md);
     box-shadow: var(--shadow-md);
   }
   ```

### 2.3 `getRecent()` stub — **תוקן: אין למחוק**

**קובץ**: `client/src/app/services/submissions.service.ts`

⚠️ **תיקון קריטי**: בדיקה קודמת (subagent) קבעה בטעות שאין קורא ל-`getRecent()`. בפועל **יש** — [dashboard.component.ts:209](../../client/src/app/pages/dashboard/dashboard.component.ts#L209) קורא לו בתוך `forkJoin` כדי לחשב את ה-KPIs "הגשות אחרונות" ו-"ממוצע ציונים". ה-endpoint `/api/students/submissions/recent` אכן לא קיים בשרת (זו תקלה קיימת ונפרדת, לא קשורה לפעמון), אבל **מחיקת המתודה תשבור קומפילציה בדשבורד**. יש **להשאיר את `getRecent()` כפי שהוא** — לא בתחום התוכנית הזו לתקן את תקלת ה-Dashboard.

---

## Phase 3 — נגישות ולמידה (לפי master-spec §6)

- ✅ `aria-label="התראות"` על הכפתור (קיים).
- ✅ Badge מראה `unreadCount()` (סימן להתראות חדשות).
- ✅ פתיחת הפאנל (`onShow`) מפעילה `markAllRead()` → המונה מתאפס.
- ✅ ניווט מקלדת: `Escape` סוגר overlay panel (PrimeNG native).
- ✅ מצב ריק: "אין התראות חדשות".
- ✅ תאריך בפורמט האחיד `dd.MM.yy HH:mm` (**תוקן**: זה הפורמט האחיד בכל הקוד הקיים, לא `dd/MM/yyyy HH:mm`).
- ✅ כיבוד `prefers-reduced-motion` (כבר גלובלי).
- ✅ סמנטיקה ARIA: `role="list"` על ה-`<ul>`.

---

## סדר עבודה מוצע

### Phase 1A — שרת: repository + query/handler + controller

1. הוסף `GradedAt` ל-`Submission.cs` (**תוקן**: נבדק מול הקוד — השדה לא קיים היום, ההוספה הכרחית ולא אופציונלית; `MarkDone` לא קובע כיום שום timestamp).
2. EF migration: מתוך `server/` —
   ```
   dotnet ef migrations add AddGradedAtToSubmission --project Infrastructure --startup-project Api
   dotnet ef database update --project Infrastructure --startup-project Api
   ```
   (**תוקן**: נדרשים ה-flags `--project`/`--startup-project` — כך מריצים מיגרציות בפועל בפרויקט הזה, ראה `server/CLAUDE.md`.)
3. שדרג את `ISubmissionRepository` עם `GetRecentGradedAsync` (דרך subagent `phase-repository-implementation`).
4. מימוש ב-`SubmissionRepository` — שימוש בשדה `_context` (**תוקן**, לא `_db`).
5. צור query + handler בתיקיה `UseCases/Notifications/GetRecentGradedSubmissions` (דרך subagent `phase-query-handler-implementation`) — קובץ ה-handler נקרא `GetRecentGradedSubmissionsHandler.cs` (**תוקן**, לא `...QueryHandler.cs`).
6. צור `NotificationsController` עם `[HttpGet("graded-submissions")]` ו-`[Authorize(Roles = "Teacher,Admin")]` (**תוקן**, לא `"Teacher"` בלבד).
7. `dotnet build server/SmartGrader.sln` — לוודא שאין שגיאות קומפילציה.
8. בדיקה: הרץ את השרת, בדוק `/api/notifications/graded-submissions` ב-Swagger — 200 למורה/Admin, 403 לתלמיד/ה.

### Phase 1B — קליינט: שירות + UI

1. צור `NotificationsService` עם polling + signals.
2. עדכן `topbar.component.ts` — עריכה ממוקדת בבלוק כפתור הפעמון בלבד (**תוקן**: לא להחליף את כל הקומפוננטה — יש לשמר את לוגו ה-brand ואת הניווט/לינק-Admin הקיימים, ראה §2.2).
3. בדיקה: הרץ `ng serve`, פתח את ה-app, בדוק שהפעמון מוצג למורה, שהבadge מתעדכן, ושלוגו ה-brand וניווט ה-Admin עדיין מוצגים כרגיל.

### Phase 1C — סיום

1. **אין** למחוק את `getRecent()` ב-`submissions.service.ts` — `dashboard.component.ts:209` קורא לו (**תוקן**, בניגוד למה שנכתב קודם).
2. בדיקה: `ng lint`, `ng build` (אין שגיאות).
3. עדכן את [docs/ux/master-spec.md](../../docs/ux/master-spec.md#L169-L170) מ"עתידי" ל"ממומש" (**Skill**: `ux-master-spec-pattern`) — הסר את שורת "התראות (פעמון)" מרשימת הפיצ'רים העתידיים ב-§7.

---

## שדרוגים עתידיים (מעבר ל-MVP)

### Upgrade A: Real-time (SignalR)

החלף polling ב-SignalR hub שה-`AiWorker` דוחף אליו כשהגשה מסתיימת:

- `NotificationsService` מרשמת ל-`notification-hub` ברגע ש-`AiWorker` קורא `submission.MarkDone()`.
- זמן response: 100ms במקום 30 שניות.

### Upgrade B: Cross-device read-state (גישה B)

מעקב "נקרא" בשרת:

- הוסף טבלה `NotificationRead { Id, StudentId, SubmissionId, ReadAt }`.
- Controller endpoint: `POST /api/notifications/{submissionId}/mark-read`.
- `NotificationsService` שולח `POST` בלחיצה על פריט.
- קרא `GetRecentGradedSubmissionsQuery`: לך עם `LEFT JOIN NotificationRead... WHERE NotificationRead.Id IS NULL` → הגשות שלא-נקראו בשום מכשיר.

### Upgrade C: Filtering & Grouping

- Filter: "Pending", "Done", "Failed".
- Group by: "Student", "Assignment", "Date".

---

## הערות

- **Backend — Skills**: Repository → Query → Controller עוקבת [backend-repository-query-pattern](../../.github/skills/backend-repository-query-pattern/SKILL.md), [backend-mediatr-query-handler-pattern](../../.github/skills/backend-mediatr-query-handler-pattern/SKILL.md), [backend-controller-endpoint-pattern](../../.github/skills/backend-controller-endpoint-pattern/SKILL.md). `backend-automapper-profile-pattern` **לא** נדרש — אין שינוי ב-DTO/profile, ה-`SubmissionResponseDto` הקיים מספיק.
- **Backend — Agents**: `phase-repository-implementation` לשלב 1.2, `phase-query-handler-implementation` לשלב 1.3 (שני subagents מצומצמי-scope שכבר שימשו לפיצ'רים דומים — `hebrew-dates-builder`, `lesson-result-progress-builder`). שלבי ה-Domain entity (1.1), ה-migration, וה-Controller (1.4) מבוצעים ישירות — אין להם agent ייעודי.
- **Client — Skills**: `client-design-token-rollout-pattern` לבלוק ה-styles ב-2.2 בלבד. `client-flow-fix-implementation-pattern` **לא** רלוונטי כאן — מוגבל ל-`[Fix]` items מתוך flow-doc קיים לפיצ'ר קיים, ואילו הפעמון הוא UI חדש לגמרי. אין skill ייעודי ל-`NotificationsService` (שירות HTTP חדש) — עוקב אחרי [client/CLAUDE.md](../../client/CLAUDE.md) הכללי.
- **Client — Agents**: אין agent ייעודי לשלב 2 כולו — מבוצע ישירות (ראה נימוק לעיל).
- **Docs**: `ux-master-spec-pattern` לעדכון §7 ב-Phase 3.
- **Client בחירה**: Signals + polling עוקבת pattern ב-Angular 17 standalone; עדיפות לחיסכון בבנדל דורש subscription pattern.
- **Polling interval**: 30 שניות היא reasonable ל-MVP; ניתן להוריד ל-15 שנ' או להעלות ל-60 שנ' לפי הצורך.
- **Error handling**: Polling failures מטופלות בשקט (catchError → of([])); לא מציגים טוסט כי זו background task.
