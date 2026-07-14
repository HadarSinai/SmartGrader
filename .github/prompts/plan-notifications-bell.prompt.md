# תוכנית מימוש: פעמון התראות

> זו תוכנית שלמה למימוש פיצ'ר התראות (Notifications Bell) בפעמון ב-topbar, לפי האפיון ב-master-spec §7.
> משדה יום: 2026-07-14 | ארכיטקטורה: Clean Architecture + CQRS (שרת), standalone components + signals (קליינט)

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

**Skill**: `backend-repository-query-pattern`

**קובץ**: `server/Domain/Abstractions/ISubmissionRepository.cs`

הוסף חתימת מתודה:

```csharp
Task<IReadOnlyList<Submission>> GetRecentGradedAsync(int limit, CancellationToken ct);
```

**קובץ**: `server/Infrastructure/Repositories/SubmissionRepository.cs`

מימוש:

```csharp
public async Task<IReadOnlyList<Submission>> GetRecentGradedAsync(int limit, CancellationToken ct) =>
    await _db.Submissions
        .AsNoTracking()
        .Include(s => s.Student)
        .Include(s => s.Assignment)
        .Where(s => s.Status == SubmissionStatus.Done)
        .OrderByDescending(s => s.GradedAt ?? s.SubmittedAt)
        .Take(limit)
        .ToListAsync(ct);
```

### 1.3 CQRS Query + Handler

**Skill**: `backend-mediatr-query-handler-pattern`

**תיקיה חדשה**: `server/Application/UseCases/Notifications/GetRecentGradedSubmissions/`

**קובץ**: `GetRecentGradedSubmissionsQuery.cs`

```csharp
using MediatR;
using SmartGrader.Application.Dtos.Submissions;

namespace SmartGrader.Application.UseCases.Notifications.GetRecentGradedSubmissions;

public record GetRecentGradedSubmissionsQuery(int Limit = 20)
    : IRequest<IReadOnlyList<SubmissionResponseDto>>;
```

**קובץ**: `GetRecentGradedSubmissionsQueryHandler.cs`

```csharp
using AutoMapper;
using MediatR;
using SmartGrader.Application.Dtos.Submissions;
using SmartGrader.Domain.Abstractions;

namespace SmartGrader.Application.UseCases.Notifications.GetRecentGradedSubmissions;

public class GetRecentGradedSubmissionsQueryHandler
    : IRequestHandler<GetRecentGradedSubmissionsQuery, IReadOnlyList<SubmissionResponseDto>>
{
    private readonly ISubmissionRepository _repo;
    private readonly IMapper _mapper;

    public GetRecentGradedSubmissionsQueryHandler(ISubmissionRepository repo, IMapper mapper)
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

**Skill**: `backend-controller-endpoint-pattern`

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
[Authorize(Roles = "Teacher")]
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

### 2.2 עדכון topbar component

**Skill**: `client-flow-fix-implementation-pattern`

**קובץ**: `client/src/app/components/layout/topbar.component.ts`

החלף את כפתור הפעמון ב-overlay panel עם list של התראות:

```typescript
import {
  Component,
  OnInit,
  ViewChild,
  EventEmitter,
  Output,
} from "@angular/core";
import { Router, RouterModule } from "@angular/router";
import { AvatarModule } from "primeng/avatar";
import { ButtonModule } from "primeng/button";
import { ToolbarModule } from "primeng/toolbar";
import { TooltipModule } from "primeng/tooltip";
import { BadgeModule } from "primeng/badge";
import { OverlayPanelModule } from "primeng/overlaypanel";
import { OverlayPanel } from "primeng/overlaypanel";
import { CommonModule } from "@angular/common";
import { AuthService } from "../../services/auth.service";
import { NotificationsService } from "../../services/notifications.service";

@Component({
  selector: "app-topbar",
  standalone: true,
  imports: [
    CommonModule,
    ButtonModule,
    AvatarModule,
    RouterModule,
    ToolbarModule,
    TooltipModule,
    BadgeModule,
    OverlayPanelModule,
  ],
  template: `
    <p-toolbar class="sg-topbar" aria-label="סרגל עליון">
      <div class="p-toolbar-group-left"></div>

      <div class="p-toolbar-group-center">
        @if (auth.isTeacher()) {
          <nav class="sg-nav" aria-label="ניווט ראשי">
            <a
              routerLink="/"
              routerLinkActive="active"
              [routerLinkActiveOptions]="{ exact: true }"
              >לוח בקרה</a
            >
            <a routerLink="/students" routerLinkActive="active">סטודנטים</a>
            <a routerLink="/assignments" routerLinkActive="active">תרגילים</a>
            <a routerLink="/lessons" routerLinkActive="active">שיעורים</a>
            <a routerLink="/submissions" routerLinkActive="active">הגשות</a>
          </nav>
        }
      </div>

      <div class="p-toolbar-group-left flex align-items-center gap-2">
        <!-- Notifications Bell -->
        @if (auth.isTeacher()) {
          <p-button
            #notifBtn
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
                      [routerLink]="[
                        '/students',
                        n.studentId,
                        'submissions',
                        n.id,
                      ]"
                      (click)="notifPanel.hide()"
                    >
                      <span class="sg-notif-text">
                        ההגשה של {{ n.studentName }} בתרגיל "{{
                          n.assignmentName
                        }}" נבדקה
                      </span>
                      <span class="sg-notif-time">
                        {{ n.submittedAt | date: "dd/MM/yyyy HH:mm" }}
                      </span>
                    </a>
                  </li>
                }
              </ul>
            }
          </p-overlayPanel>
        }

        <!-- Avatar & Logout -->
        <div class="flex align-items-center gap-2">
          <p-avatar
            [label]="avatarInitial()"
            shape="circle"
            [style]="{
              'background-color': 'var(--accent)',
              color: 'var(--accent-ink)',
            }"
          >
          </p-avatar>
          <span class="sg-topbar-user">{{ auth.fullName() }}</span>
          <p-button
            icon="pi pi-sign-out"
            [text]="true"
            [rounded]="true"
            severity="secondary"
            ariaLabel="התנתקות"
            pTooltip="התנתקות"
            tooltipPosition="bottom"
            (onClick)="logout()"
          >
          </p-button>
        </div>
      </div>
    </p-toolbar>
  `,
  styles: [
    `
      .sg-topbar-user {
        font-weight: 600;
        color: var(--app-text-strong);
        white-space: nowrap;
      }

      @media (max-width: 420px) {
        .sg-topbar-user {
          display: none;
        }
      }

      .sg-notif-empty {
        padding: var(--space-md);
        text-align: center;
        color: var(--app-text-weak);
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
        gap: var(--space-xs);
        padding: var(--space-md);
        text-decoration: none;
        color: var(--app-text);
        transition: background-color 0.2s;
      }

      .sg-notif-item a:hover {
        background-color: var(--app-bg-hover);
      }

      .sg-notif-text {
        font-weight: 500;
        line-height: 1.4;
      }

      .sg-notif-time {
        font-size: var(--text-xs);
        color: var(--app-text-weak);
      }

      :global(.sg-notif-panel .p-overlaypanel-content) {
        padding: 0;
        border-radius: var(--radius-md);
        box-shadow: var(--shadow-md);
      }
    `,
  ],
})
export class TopbarComponent implements OnInit {
  @Output() menuClick = new EventEmitter<void>();
  @ViewChild("notifPanel") notifPanel!: OverlayPanel;

  constructor(
    public auth: AuthService,
    public notifications: NotificationsService,
    private router: Router,
  ) {}

  ngOnInit(): void {
    // Start polling for notifications (teachers only)
    this.notifications.start();
  }

  avatarInitial(): string {
    return this.auth.fullName().charAt(0) || "?";
  }

  toggleNotifications(event: Event): void {
    if (this.notifPanel) {
      this.notifPanel.toggle(event);
    }
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(["/login"]);
  }
}
```

### 2.3 עדכון ה-service הקיים

**קובץ**: `client/src/app/services/submissions.service.ts`

מחק או תאם את ה-stub `getRecent()`:

```typescript
// DELETE THIS (or adapt if actually needed elsewhere):
// getRecent(limit: number): Observable<SubmissionResponseDto[]> { ... }
```

---

## Phase 3 — נגישות ולמידה (לפי master-spec §6)

- ✅ `aria-label="התראות"` על הכפתור (קיים).
- ✅ Badge מראה `unreadCount()` (סימן להתראות חדשות).
- ✅ פתיחת הפאנל (`onShow`) מפעילה `markAllRead()` → המונה מתאפס.
- ✅ ניווט מקלדת: `Escape` סוגר overlay panel (PrimeNG native).
- ✅ מצב ריק: "אין התראות חדשות".
- ✅ תאריך בפורמט האחיד `dd/MM/yyyy HH:mm`.
- ✅ כיבוד `prefers-reduced-motion` (כבר גלובלי).
- ✅ סמנטיקה ARIA: `role="list"` על ה-`<ul>`.

---

## סדר עבודה מוצע

### Phase 1A — שרת: repository + query/handler + controller

1. הוסף `GradedAt` ל-`Submission.cs` (אופציונלי; בלעדיו השתמש ב-`SubmittedAt` למיון).
2. EF migration (אם הוספת `GradedAt`): `dotnet ef migrations add AddGradedAtToSubmission`
3. שדרג את `ISubmissionRepository` עם `GetRecentGradedAsync`.
4. מימוש ב-`SubmissionRepository`.
5. צור query + handler בתיקיה `UseCases/Notifications/GetRecentGradedSubmissions`.
6. צור `NotificationsController` עם `[HttpGet("graded-submissions")]`.
7. בדיקה: הרץ את השרת, בדוק `/api/notifications/graded-submissions` ב-Swagger (teacher role נדרש).

### Phase 1B — קליינט: שירות + UI

1. צור `NotificationsService` עם polling + signals.
2. עדכן `topbar.component.ts` — החלף כפתור ב-overlay panel + badge.
3. בדיקה: הרץ `ng serve`, פתח את ה-app, בדוק שהפעמון מוצג למורה, שהבadge מתעדכן.

### Phase 1C — סיום

1. מחק/תאם `getRecent()` stub ב-`submissions.service.ts`.
2. בדיקה: `ng lint`, `ng build` (אין שגיאות).
3. עדכן את [docs/ux/master-spec.md](../../docs/ux/master-spec.md#L169-L170) מ"עתידי" ל"ממומש".

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

- **Backend اختیار**: Repository → Query → Controller עוקבת [backend-repository-query-pattern](../../.github/skills/backend-repository-query-pattern/SKILL.md), [backend-mediatr-query-handler-pattern](../../.github/skills/backend-mediatr-query-handler-pattern/SKILL.md), [backend-controller-endpoint-pattern](../../.github/skills/backend-controller-endpoint-pattern/SKILL.md).
- **Client בחירה**: Signals + polling עוקבת pattern ב-Angular 17 standalone; עדיפות לחיסכון בבנדל דורש subscription pattern.
- **Polling interval**: 30 שניות היא reasonable ל-MVP; ניתן להוריד ל-15 שנ' או להעלות ל-60 שנ' לפי הצורך.
- **Error handling**: Polling failures מטופלות בשקט (catchError → of([])); לא מציגים טוסט כי זו background task.
