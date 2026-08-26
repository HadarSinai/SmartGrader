import { CommonModule } from "@angular/common";
import { Component, OnDestroy, OnInit, inject } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";
import {
  STATUS_LABELS_HE,
  SubmissionResponseDto,
} from "@models/submission.model";
import { SubmissionsService } from "@services/submissions.service";
import { MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { DialogModule } from "primeng/dialog";
import { InputNumberModule } from "primeng/inputnumber";
import { InputTextareaModule } from "primeng/inputtextarea";
import { TagModule } from "primeng/tag";
import { SubmissionFeedbackPanelComponent } from "@components/submission-feedback-panel/submission-feedback-panel.component";

@Component({
  selector: "app-submission-detail",
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    CardModule,
    ButtonModule,
    DialogModule,
    InputNumberModule,
    InputTextareaModule,
    TagModule,
    SubmissionFeedbackPanelComponent,
  ],
  template: `
    <section class="sg-page">
      <div class="pt-3 pb-5">
        <p-card styleClass="sg-card">
          <ng-template pTemplate="header">
            <div
              class="flex flex-column md:flex-row md:align-items-end md:justify-content-between gap-3 px-4 pt-4 pb-2"
            >
              <div class="sg-title">
                <a
                  class="sg-breadcrumb-link"
                  role="link"
                  tabindex="0"
                  (click)="navigateToList()"
                  (keydown.enter)="navigateToList()"
                >
                  <i class="pi pi-arrow-right" aria-hidden="true"></i>
                  חזרה להגשות
                </a>
                <div class="sg-h1">פרטי הגשה</div>
                <div class="sg-h2">צפייה בפרטים, סטטוס וקוד</div>
              </div>

              <div class="flex align-items-center gap-2 flex-wrap">
                <!-- ⚠️ כפתור ולא קוד משותף: קוד עובר בין תלמידות ומנטרל את הכלל בשקט.
                     ההגנה כאן היא ההזדהות הקיימת, וכל אישור נרשם ביומן ביקורת.
                     ⚠️ lockReason מוציא מכלל זה שיעור שסוכם או כיתה בארכיון: שם האישור
                     לא יפתח דבר (נעילה גוברת גם עליו — ר' Submission.MarkPendingAi),
                     והצעתו הייתה מבטיחה למורה פעולה שתחזור כשגיאה. -->
                <p-button
                  *ngIf="submission && !submission.canResubmit && !submission.lockReason"
                  label="אישור הגשה נוספת"
                  icon="pi pi-unlock"
                  [outlined]="true"
                  (onClick)="openGrantExtraAttempt()"
                ></p-button>

                <span
                  *ngIf="submission?.lockReason"
                  class="sg-lock-note"
                  ><i class="pi pi-lock" aria-hidden="true"></i>
                  {{ submission!.lockReason }}</span
                >
                <p-button
                  *ngIf="canOverrideScore"
                  label="דריסת ציון"
                  icon="pi pi-sliders-h"
                  [outlined]="true"
                  (onClick)="openOverrideScore()"
                ></p-button>
                <!-- ⚠️ "עריכה" מוליכה ל-UpdateSubmission, וזה זורק על הגשה נעולה
                     (SubmissionLock.Message). המסך הסביר כבר למה — אין להציע פעולה
                     שהמסך עצמו הרגע פסל. -->
                <p-button
                  *ngIf="!submission?.lockReason"
                  label="עריכה"
                  icon="pi pi-pencil"
                  styleClass="sg-btn-primary"
                  (onClick)="navigateToEdit()"
                >
                </p-button>
              </div>
            </div>
          </ng-template>

          <div class="px-4 pb-4" *ngIf="submission">
            <div class="grid">
              <div class="col-12 md:col-6">
                <div class="text-xs font-bold text-color-secondary mb-1">
                  סטודנט
                </div>
                <div class="text-color font-semibold">
                  {{ submission.studentName || "—" }}
                </div>
              </div>

              <div class="col-12 md:col-6">
                <div class="text-xs font-bold text-color-secondary mb-1">
                  תרגיל
                </div>
                <div class="text-color font-semibold">
                  {{ submission.assignmentName || "—" }}
                </div>
              </div>

              <div class="col-12 md:col-6">
                <div class="text-xs font-bold text-color-secondary mb-1">
                  נשלח
                </div>
                <div class="text-color">
                  {{ submission.submittedAt | date: "dd.MM.yy HH:mm" }}
                </div>
              </div>

              <div class="col-12 md:col-6">
                <div class="text-xs font-bold text-color-secondary mb-1">
                  ציון
                </div>
                <div
                  class="text-3xl font-bold"
                  [class.opacity-70]="submission.score === null"
                  style="color: var(--accent)"
                >
                  {{ submission.score ?? "—" }}
                </div>
                <div
                  class="text-color-secondary text-sm"
                  *ngIf="submission.score === null"
                >
                  {{
                    submission.status === "RequirementsNotMet"
                      ? "לא ניתן ציון — דרישה חוסמת לא התקיימה"
                      : "עדיין לא נבדק"
                  }}
                </div>
                <!-- ציון שנדרס נראה זהה לציון מחושב, ולכן הסיבה מוצגת לצידו תמיד -->
                <div
                  class="text-color-secondary text-sm"
                  *ngIf="submission.scoreOverrideReason"
                >
                  <i class="pi pi-sliders-h" aria-hidden="true"></i>
                  ציון שנקבע ידנית: {{ submission.scoreOverrideReason }}
                </div>
                <div
                  class="text-color-secondary text-sm"
                  *ngIf="submission.hasUnusedExtraAttempt"
                >
                  <i class="pi pi-unlock" aria-hidden="true"></i>
                  אושרה הגשה נוספת<ng-container
                    *ngIf="submission.extraAttemptReason"
                    >: {{ submission.extraAttemptReason }}</ng-container
                  >
                </div>
              </div>

              <!-- ציר הניסיונות. ⚠️ רק האחרון נחשב כציון — עד שנוסף הארכיון, הגשה חוזרת
                   דרסה את הקודמת במקום והמידע נעלם. -->
              <div class="col-12" *ngIf="submission.attempts?.length">
                <div class="text-xs font-bold text-color-secondary mb-2">
                  ניסיונות
                </div>
                <ol class="sg-attempts">
                  <li class="sg-attempts__item sg-attempts__item--current">
                    <span class="sg-attempts__no"
                      >ניסיון {{ submission.attemptNumber }}</span
                    >
                    <span class="sg-attempts__score">{{
                      submission.score ?? "—"
                    }}</span>
                    <span class="sg-attempts__meta">
                      {{ submission.submittedAt | date: "dd.MM.yy HH:mm" }} ·
                      נוכחי, זה הציון שנחשב
                    </span>
                  </li>
                  <li
                    class="sg-attempts__item"
                    *ngFor="let attempt of submission.attempts"
                  >
                    <span class="sg-attempts__no"
                      >ניסיון {{ attempt.attemptNumber }}</span
                    >
                    <span class="sg-attempts__score">{{
                      attempt.score ?? "—"
                    }}</span>
                    <span class="sg-attempts__meta">
                      {{ attempt.submittedAt | date: "dd.MM.yy HH:mm" }} ·
                      {{ statusLabels[attempt.status] || attempt.status }}
                      <ng-container *ngIf="attempt.isCollapsed"
                        >· הפרטים המלאים נגזמו</ng-container
                      >
                    </span>
                  </li>
                </ol>
              </div>

              <!-- Unified status area -->
              <div class="col-12">
                <div
                  class="sg-status-box"
                  [class.sg-status-box--error]="
                    submission.status === 'CompilationFailed' ||
                    submission.status === 'AiFailed'
                  "
                  [class.sg-status-box--warn]="
                    submission.status === 'JudgeUnavailable'
                  "
                >
                  <div class="flex align-items-center gap-2 flex-wrap">
                    <ng-container [ngSwitch]="submission.status">
                      <p-tag
                        *ngSwitchCase="'Done'"
                        severity="success"
                        [value]="statusLabels['Done']"
                        icon="pi pi-check-circle"
                      />
                      <p-tag
                        *ngSwitchCase="'PendingAi'"
                        severity="warning"
                        [value]="statusLabels['PendingAi']"
                        icon="pi pi-clock"
                      />
                      <p-tag
                        *ngSwitchCase="'ProcessingAi'"
                        severity="info"
                        [value]="statusLabels['ProcessingAi']"
                        icon="pi pi-spinner pi-spin"
                      />
                      <p-tag
                        *ngSwitchCase="'AiFailed'"
                        severity="danger"
                        [value]="statusLabels['AiFailed']"
                        icon="pi pi-exclamation-triangle"
                      />
                      <p-tag
                        *ngSwitchCase="'CompilationFailed'"
                        severity="danger"
                        [value]="statusLabels['CompilationFailed']"
                        icon="pi pi-times-circle"
                      />
                      <p-tag
                        *ngSwitchCase="'JudgeUnavailable'"
                        severity="warning"
                        [value]="statusLabels['JudgeUnavailable']"
                        icon="pi pi-exclamation-circle"
                      />
                      <p-tag
                        *ngSwitchCase="'RequirementsNotMet'"
                        severity="danger"
                        [value]="statusLabels['RequirementsNotMet']"
                        icon="pi pi-ban"
                      />
                      <p-tag
                        *ngSwitchDefault
                        [value]="submission.status || 'לא ידוע'"
                        severity="secondary"
                      />
                    </ng-container>

                    <span
                      class="text-color-secondary text-sm"
                      *ngIf="isPolling"
                      aria-live="polite"
                    >
                      מתעדכן אוטומטית...
                    </span>
                  </div>

                  <div
                    *ngIf="
                      submission.status === 'CompilationFailed' &&
                      submission.compileError
                    "
                  >
                    <strong>שגיאת קומפילציה:</strong>
                    <pre>{{ submission.compileError }}</pre>
                  </div>

                  <div
                    *ngIf="
                      submission.status === 'JudgeUnavailable' &&
                      submission.aiError
                    "
                  >
                    <strong>פרטי התקלה (לא קשורה לקוד של התלמיד):</strong>
                    <pre>{{ submission.aiError }}</pre>
                  </div>

                  <div
                    *ngIf="
                      submission.status !== 'JudgeUnavailable' &&
                      submission.aiError
                    "
                  >
                    <strong>שגיאת AI:</strong>
                    <pre>{{ submission.aiError }}</pre>
                  </div>

                  <!-- ⚠️ גם ב-RequirementsNotMet: שם אין תוצאות בדיקות (Judge0 לא רץ),
                       אבל טבלת הדרישות היא כל ההסבר לְמה שקרה. -->
                  <app-submission-feedback-panel
                    *ngIf="
                      submission.status === 'Done' ||
                      submission.status === 'RequirementsNotMet'
                    "
                    [submission]="submission"
                  ></app-submission-feedback-panel>

                  <!-- ⚠️ אותה נעילה כמו ב-"עריכה" שבכותרת: שיעור שסוכם דוחה גם הגשה
                       חוזרת אחרי כשל קומפילציה. -->
                  <div
                    *ngIf="
                      !submission.lockReason &&
                      (submission.status === 'CompilationFailed' ||
                        submission.status === 'AiFailed' ||
                        submission.status === 'JudgeUnavailable')
                    "
                  >
                    <p-button
                      label="עריכה והגשה מחדש"
                      icon="pi pi-refresh"
                      styleClass="sg-btn-primary"
                      (onClick)="navigateToEdit()"
                    ></p-button>
                  </div>
                </div>
              </div>

              <div class="col-12">
                <div class="text-xs font-bold text-color-secondary mb-2">
                  קוד
                </div>
                <pre class="sg-code-box">{{ submission.sourceCode }}</pre>
              </div>
            </div>
          </div>

          <div
            class="flex align-items-center justify-content-center py-6"
            *ngIf="loading"
          >
            <i class="pi pi-spin pi-spinner text-3xl" aria-hidden="true"></i>
          </div>
        </p-card>
      </div>

      <!-- אישור הגשה נוספת. ⚠️ הסיבה היא שדה חובה — היא מה שמחליף את "לראות מי
           השתמשה בקוד המשותף", ובלעדיה ליומן הביקורת אין ערך. -->
      <p-dialog
        header="אישור הגשה נוספת"
        [(visible)]="grantDialogOpen"
        [modal]="true"
        [style]="{ width: '28rem' }"
        [draggable]="false"
        [resizable]="false"
      >
        <div class="flex flex-column gap-3">
          <div>
            האישור גובר על סף הציון של התרגיל ומאפשר לתלמידה להגיש פעם נוספת. הוא
            חד-פעמי ונצרך בהגשה הבאה.
          </div>
          <div>
            <label class="sg-label" for="grantReason">סיבה *</label>
            <textarea
              pInputTextarea
              class="w-full"
              id="grantReason"
              rows="3"
              [(ngModel)]="grantReason"
              placeholder="לדוגמה: תקלה בהגשה בשיעור"
            ></textarea>
            <small class="p-error block" *ngIf="grantAttempted && !grantReason.trim()">
              יש לציין סיבה — היא נשמרת ביומן הביקורת.
            </small>
          </div>
        </div>
        <ng-template pTemplate="footer">
          <p-button
            label="ביטול"
            severity="secondary"
            [outlined]="true"
            (onClick)="grantDialogOpen = false"
          ></p-button>
          <p-button
            label="אישור"
            styleClass="sg-btn-primary"
            [loading]="grantSaving"
            (onClick)="saveGrantExtraAttempt()"
          ></p-button>
        </ng-template>
      </p-dialog>

      <!-- דריסת ציון — רשת ביטחון, לא חלק מהמסלול הרגיל -->
      <p-dialog
        header="דריסת ציון"
        [(visible)]="overrideDialogOpen"
        [modal]="true"
        [style]="{ width: '28rem' }"
        [draggable]="false"
        [resizable]="false"
      >
        <div class="flex flex-column gap-3">
          <div>
            הציון שיוזן יחליף את הציון המחושב, והסיבה תישמר לצידו ותוצג במסך הזה.
          </div>
          <div>
            <label class="sg-label" for="overrideScore">ציון *</label>
            <p-inputNumber
              inputId="overrideScore"
              [(ngModel)]="overrideScore"
              [min]="0"
              [showButtons]="true"
              styleClass="w-full"
            ></p-inputNumber>
          </div>
          <div>
            <label class="sg-label" for="overrideReason">סיבה *</label>
            <textarea
              pInputTextarea
              class="w-full"
              id="overrideReason"
              rows="3"
              [(ngModel)]="overrideReason"
              placeholder="לדוגמה: הפלט הצפוי בבדיקה 2 היה שגוי"
            ></textarea>
            <small
              class="p-error block"
              *ngIf="overrideAttempted && !overrideReason.trim()"
            >
              יש לציין סיבה — היא נשמרת ביומן הביקורת.
            </small>
          </div>
        </div>
        <ng-template pTemplate="footer">
          <p-button
            label="ביטול"
            severity="secondary"
            [outlined]="true"
            (onClick)="overrideDialogOpen = false"
          ></p-button>
          <p-button
            label="שמירה"
            styleClass="sg-btn-primary"
            [loading]="overrideSaving"
            [disabled]="overrideScore === null"
            (onClick)="saveOverrideScore()"
          ></p-button>
        </ng-template>
      </p-dialog>
    </section>
  `,
  styles: [
    `
      .sg-lock-note {
        display: inline-flex;
        align-items: center;
        gap: var(--space-2);
        padding: var(--space-2) var(--space-3);
        border-radius: var(--radius-md);
        background: var(--app-surface-2);
        color: var(--app-text-muted);
        font-size: var(--text-sm);
      }

      .sg-attempts {
        list-style: none;
        margin: 0;
        padding: 0;
        display: flex;
        flex-direction: column;
        gap: var(--space-1);
      }

      .sg-attempts__item {
        display: flex;
        flex-wrap: wrap;
        align-items: baseline;
        gap: var(--space-2);
        padding: var(--space-2) var(--space-3);
        border-radius: var(--radius-md);
        background: var(--app-surface-2);
      }

      .sg-attempts__item--current {
        border: 1px solid var(--accent);
      }

      .sg-attempts__no {
        font-weight: 700;
        min-width: 5rem;
      }

      .sg-attempts__score {
        font-weight: 800;
        color: var(--accent);
        min-width: 2.5rem;
      }

      .sg-attempts__meta {
        font-size: var(--text-sm);
        color: var(--app-muted);
      }

      .sg-status-box pre {
        font-family: "Courier New", Courier, monospace;
        font-size: 0.875rem;
        margin: 6px 0 0;
        white-space: pre-wrap;
        word-break: break-word;
        direction: ltr;
        text-align: left;
      }
    `,
  ],
})
export class SubmissionDetailComponent implements OnInit, OnDestroy {
  private readonly submissionsService = inject(SubmissionsService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly messageService = inject(MessageService);

  private pollHandle: ReturnType<typeof setInterval> | null = null;

  submission: SubmissionResponseDto | null = null;
  loading = false;
  isPolling = false;
  studentId!: number;
  submissionId!: number;
  readonly statusLabels = STATUS_LABELS_HE;

  // ── דריסות המורה ──────────────────────────────────────────────────────
  grantDialogOpen = false;
  grantReason = "";
  grantSaving = false;
  grantAttempted = false;

  overrideDialogOpen = false;
  overrideScore: number | null = null;
  overrideReason = "";
  overrideSaving = false;
  overrideAttempted = false;

  /** אין מה לדרוס בזמן שההגשה עדיין בבדיקה — הציון שייכתב יידרס מיד אחר כך. */
  get canOverrideScore(): boolean {
    const status = this.submission?.status;
    return !!status && status !== "PendingAi" && status !== "ProcessingAi";
  }

  openGrantExtraAttempt(): void {
    this.grantReason = "";
    this.grantAttempted = false;
    this.grantDialogOpen = true;
  }

  saveGrantExtraAttempt(): void {
    this.grantAttempted = true;
    if (!this.grantReason.trim()) return;

    this.grantSaving = true;
    this.submissionsService
      .grantExtraAttempt(this.studentId, this.submissionId, {
        reason: this.grantReason.trim(),
      })
      .subscribe({
        next: (updated: SubmissionResponseDto) => {
          this.submission = updated;
          this.grantSaving = false;
          this.grantDialogOpen = false;
          this.messageService.add({
            severity: "success",
            summary: "בוצע",
            detail: "האישור נשמר. התלמידה יכולה להגיש פעם נוספת.",
          });
        },
        error: () => {
          // ApiErrorInterceptor כבר מציג את הודעת השרת (למשל שיעור שהסתיים, שנפתח
          // מחדש דרך "פתיחה מחדש" ולא דרך אישור הגשה) — אין כאן טוסט כפול.
          this.grantSaving = false;
        },
      });
  }

  openOverrideScore(): void {
    this.overrideScore = this.submission?.score ?? null;
    this.overrideReason = "";
    this.overrideAttempted = false;
    this.overrideDialogOpen = true;
  }

  saveOverrideScore(): void {
    this.overrideAttempted = true;
    if (this.overrideScore === null || !this.overrideReason.trim()) return;

    this.overrideSaving = true;
    this.submissionsService
      .overrideScore(this.studentId, this.submissionId, {
        score: this.overrideScore,
        reason: this.overrideReason.trim(),
      })
      .subscribe({
        next: (updated: SubmissionResponseDto) => {
          this.submission = updated;
          this.overrideSaving = false;
          this.overrideDialogOpen = false;
          this.messageService.add({
            severity: "success",
            summary: "בוצע",
            detail: "הציון עודכן",
          });
        },
        error: () => {
          // התקרה תלויה בתרגיל (בונוס), ולכן היא נבדקת בשרת וההודעה מגיעה משם.
          this.overrideSaving = false;
        },
      });
  }

  ngOnInit(): void {
    const studentIdParam = this.route.snapshot.paramMap.get("studentId");
    const submissionIdParam = this.route.snapshot.paramMap.get("submissionId");

    if (studentIdParam && submissionIdParam) {
      this.studentId = parseInt(studentIdParam, 10);
      this.submissionId = parseInt(submissionIdParam, 10);
      this.loadSubmission();
    }
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  loadSubmission(): void {
    this.loading = true;
    this.submissionsService
      .getById(this.studentId, this.submissionId)
      .subscribe({
        next: (data: SubmissionResponseDto) => {
          this.submission = data;
          this.loading = false;
          this.syncPolling(data.status);
        },
        error: (_error: unknown) => {
          this.messageService.add({
            severity: "error",
            summary: "שגיאה",
            detail: "טעינת ההגשה נכשלה",
          });
          this.loading = false;
        },
      });
  }

  private syncPolling(status: string | null): void {
    const shouldPoll = status === "PendingAi" || status === "ProcessingAi";
    if (shouldPoll && !this.pollHandle) {
      this.isPolling = true;
      this.pollHandle = setInterval(() => this.refreshSilently(), 7000);
    } else if (!shouldPoll) {
      this.stopPolling();
    }
  }

  private refreshSilently(): void {
    this.submissionsService
      .getById(this.studentId, this.submissionId)
      .subscribe({
        next: (data: SubmissionResponseDto) => {
          this.submission = data;
          this.syncPolling(data.status);
        },
        error: () => {
          // Keep polling silently; a transient error shouldn't stop the loop.
        },
      });
  }

  private stopPolling(): void {
    if (this.pollHandle) {
      clearInterval(this.pollHandle);
      this.pollHandle = null;
    }
    this.isPolling = false;
  }

  getStatusSeverity(
    status: string | null,
  ): "success" | "info" | "warning" | "danger" | "secondary" | "contrast" {
    if (!status) return "secondary";
    const statusLower = status.toLowerCase();
    if (statusLower.includes("pass") || statusLower.includes("success"))
      return "success";
    if (statusLower.includes("fail") || statusLower.includes("error"))
      return "danger";
    if (statusLower.includes("pending")) return "warning";
    return "info";
  }

  navigateToList(): void {
    this.router.navigate(["/students", this.studentId, "submissions"]);
  }

  navigateToEdit(): void {
    this.router.navigate([
      "/students",
      this.studentId,
      "submissions",
      this.submissionId,
      "edit",
    ]);
  }
}
