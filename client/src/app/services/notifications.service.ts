import { Injectable, computed, signal } from "@angular/core";
import { of, timer } from "rxjs";
import { catchError, switchMap } from "rxjs/operators";
import { SubmissionResponseDto } from "@models/submission.model";
import { ApiClient } from "../core/http/api-client";
import { AuthService } from "./auth.service";

const LAST_SEEN_KEY = "notif.lastSeenId";
const POLL_INTERVAL_MS = 30_000;

@Injectable({ providedIn: "root" })
export class NotificationsService {
  private readonly _items = signal<SubmissionResponseDto[]>([]);
  readonly items = this._items.asReadonly();

  // גישה A (MVP): "נקרא" נשמר מקומית בדפדפן, לא מסונכרן בין מכשירים.
  private readonly lastSeenId = signal<number>(
    Number(localStorage.getItem(LAST_SEEN_KEY) ?? 0),
  );
  readonly unreadCount = computed(
    () => this._items().filter((s) => s.id > this.lastSeenId()).length,
  );

  private isStarted = false;

  constructor(
    private api: ApiClient,
    private auth: AuthService,
  ) {}

  start(): void {
    if (this.isStarted || (!this.auth.isTeacher() && !this.auth.isAdmin())) {
      return;
    }
    this.isStarted = true;

    timer(0, POLL_INTERVAL_MS)
      .pipe(
        switchMap(() =>
          this.api.http.get<SubmissionResponseDto[]>(
            this.api.url("/api/notifications/graded-submissions?limit=20"),
          ),
        ),
        catchError(() => of([])),
      )
      .subscribe((list) => this._items.set(list));
  }

  markAllRead(): void {
    const maxId = Math.max(0, ...this._items().map((s) => s.id));
    this.lastSeenId.set(maxId);
    localStorage.setItem(LAST_SEEN_KEY, String(maxId));
  }
}
