import { Injectable, computed, signal } from "@angular/core";
import { Observable, of, timer } from "rxjs";
import { catchError, switchMap, tap } from "rxjs/operators";
import { ClassSignalDto } from "@models/notification.model";
import { SubmissionResponseDto } from "@models/submission.model";
import { ApiClient } from "../core/http/api-client";
import { AuthService } from "./auth.service";

const LAST_SEEN_KEY = "notif.lastSeenId";
const SEEN_SIGNALS_KEY = "notif.seenSignalKeys";
const POLL_INTERVAL_MS = 30_000;

/**
 * שני פעמונים שונים לגמרי מאחורי רכיב אחד:
 *
 * - **מורה/מנהלת** — סיגנלים על הכיתה ועל התרגילים (`/api/notifications/class-signals`).
 *   אותם סיגנלים בדיוק שנשלחים בסיכום היומי במייל, ועל אותו חלון תאריכים.
 * - **תלמידה** — ההגשות שלה שנבדקו (`/api/notifications/graded-submissions`), בלי שינוי.
 *   ⚠️ הסיגנלים חושפים כמה בנות נכשלו ובמה, ולכן אינם נמסרים לתלמידה בשום מסלול —
 *   השרת חוסם אותם ב-`[Authorize(Roles = "Teacher,Admin")]`, וזו הבקרה. הפיצול כאן
 *   הוא תצוגה בלבד.
 */
@Injectable({ providedIn: "root" })
export class NotificationsService {
  private readonly _signals = signal<ClassSignalDto[]>([]);
  readonly signals = this._signals.asReadonly();

  private readonly _graded = signal<SubmissionResponseDto[]>([]);
  readonly graded = this._graded.asReadonly();

  // גישה A (MVP): "נקרא" נשמר מקומית בדפדפן, לא מסונכרן בין מכשירים.
  private readonly lastSeenId = signal<number>(
    Number(localStorage.getItem(LAST_SEEN_KEY) ?? 0),
  );

  // ⚠️ לסיגנל אין id — אין ישות ואין שורה — ולכן "נקרא" נשמר כאוסף מפתחות ולא
  // כמספר רץ. השוואת "גדול מ-" הייתה חסרת משמעות כאן.
  private readonly seenSignalKeys = signal<ReadonlySet<string>>(
    NotificationsService.readSeenKeys(),
  );

  readonly unreadCount = computed(() =>
    this.auth.isStudent()
      ? this._graded().filter((s) => s.id > this.lastSeenId()).length
      : this._signals().filter((s) => !this.seenSignalKeys().has(s.key)).length,
  );

  private isStarted = false;

  constructor(
    private api: ApiClient,
    private auth: AuthService,
  ) {}

  start(): void {
    if (this.isStarted || !this.auth.isLoggedIn()) {
      return;
    }
    this.isStarted = true;

    timer(0, POLL_INTERVAL_MS)
      .pipe(
        switchMap(() =>
          this.auth.isStudent() ? this.fetchGraded() : this.fetchSignals(),
        ),
        catchError(() => of(null)),
      )
      .subscribe();
  }

  markAllRead(): void {
    if (this.auth.isStudent()) {
      const maxId = Math.max(0, ...this._graded().map((s) => s.id));
      this.lastSeenId.set(maxId);
      localStorage.setItem(LAST_SEEN_KEY, String(maxId));
      return;
    }

    // נשמרים בדיוק המפתחות שמוצגים כרגע, ולא איחוד עם הישנים — כך האחסון אינו
    // גדל לנצח, וסיגנל שחזר אחרי שנעלם ייחשב חדש בצדק.
    const keys = this._signals().map((s) => s.key);
    this.seenSignalKeys.set(new Set(keys));
    localStorage.setItem(SEEN_SIGNALS_KEY, JSON.stringify(keys));
  }

  private fetchSignals(): Observable<ClassSignalDto[]> {
    return this.api.http
      .get<ClassSignalDto[]>(this.api.url("/api/notifications/class-signals"))
      .pipe(
        catchError(() => of([] as ClassSignalDto[])),
        tap((list) => this._signals.set(list)),
      );
  }

  private fetchGraded(): Observable<SubmissionResponseDto[]> {
    return this.api.http
      .get<SubmissionResponseDto[]>(
        this.api.url("/api/notifications/graded-submissions?limit=20"),
      )
      .pipe(
        catchError(() => of([] as SubmissionResponseDto[])),
        tap((list) => this._graded.set(list)),
      );
  }

  private static readSeenKeys(): ReadonlySet<string> {
    try {
      const raw = localStorage.getItem(SEEN_SIGNALS_KEY);
      const parsed: unknown = raw ? JSON.parse(raw) : [];
      return new Set(Array.isArray(parsed) ? (parsed as string[]) : []);
    } catch {
      // ערך פגום ב-localStorage לא יפיל את הפעמון — הכול פשוט ייחשב חדש.
      return new Set<string>();
    }
  }
}
