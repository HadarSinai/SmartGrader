import { CommonModule } from "@angular/common";
import { Component, OnDestroy, OnInit } from "@angular/core";
import { ActivatedRoute, RouterModule } from "@angular/router";

import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { TagModule } from "primeng/tag";

import {
    STATUS_LABELS_HE,
    SubmissionResponseDto,
} from "@models/submission.model";
import { AuthService } from "@services/auth.service";
import { SubmissionsService } from "@services/submissions.service";
import { SubmissionFeedbackPanelComponent } from "@components/submission-feedback-panel/submission-feedback-panel.component";

@Component({
  selector: "app-my-feedback",
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ButtonModule,
    CardModule,
    TagModule,
    SubmissionFeedbackPanelComponent,
  ],
  template: `
    <section class="sg-page">
      <div class="pt-3 pb-5">
        <p-card styleClass="sg-card">
          <ng-template pTemplate="header">
            <div class="px-4 pt-4 pb-2">
              <a
                class="sg-breadcrumb-link"
                *ngIf="lessonId !== null"
                [routerLink]="['/my', 'lessons', lessonId, 'assignments']"
              >
                <i class="pi pi-arrow-right" aria-hidden="true"></i>
                חזרה לתרגילים
              </a>
              <a
                class="sg-breadcrumb-link"
                *ngIf="lessonId === null"
                [routerLink]="['/my', 'lessons']"
              >
                <i class="pi pi-arrow-right" aria-hidden="true"></i>
                חזרה לשיעורים שלי
              </a>
              <div class="sg-title mt-2">
                <div class="sg-h1">פידבק על ההגשה</div>
                <div class="sg-h2" *ngIf="submission?.assignmentName">
                  {{ submission!.assignmentName }}
                </div>
              </div>
            </div>
          </ng-template>

          <div *ngIf="loading" class="flex justify-content-center py-6">
            <i class="pi pi-spin pi-spinner text-3xl" aria-hidden="true"></i>
          </div>

          <div *ngIf="!loading && submission" class="grid p-4">
            <!-- Key-value grid -->
            <div class="col-12 md:col-6">
              <div class="sg-label">תרגיל</div>
              <div>{{ submission.assignmentName || "—" }}</div>
            </div>
            <div class="col-12 md:col-6">
              <div class="sg-label">הוגש בתאריך</div>
              <div>{{ submission.submittedAt | date: "dd.MM.yy HH:mm" }}</div>
            </div>
            <div class="col-12 md:col-6">
              <div class="sg-label">ציון</div>
              <div class="sg-big-score">
                {{ submission.score !== null ? submission.score : "—" }}
              </div>
              <!-- ⚠️ "אין ציון" ולא "0": דרישה חוסמת שלא התקיימה היא דחייה, לא ציון נמוך -->
              <div
                class="sg-hint"
                *ngIf="submission.status === 'RequirementsNotMet'"
              >
                לא ניתן ציון — התרגיל לא נבדק מול מקרי הבדיקה.
              </div>
              <div class="sg-hint" *ngIf="submission.attemptNumber > 1">
                ניסיון {{ submission.attemptNumber }}. רק הניסיון האחרון נחשב.
              </div>
            </div>

            <!-- אישור המורה גובר על סף הציון. נאמר לתלמידה במפורש, אחרת כפתור שנפתח
                 בלי הסבר נראה כמו תקלה -->
            <div class="col-12" *ngIf="submission.hasUnusedExtraAttempt">
              <div class="sg-extra-attempt">
                <i class="pi pi-unlock" aria-hidden="true"></i>
                <span>
                  המורה אישרה לך הגשה נוספת<ng-container
                    *ngIf="submission.extraAttemptReason"
                    >: {{ submission.extraAttemptReason }}</ng-container
                  >
                </span>
              </div>
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
                [ngSwitch]="submission.status"
              >
                <!-- בבדיקה -->
                <ng-container *ngSwitchCase="'PendingAi'">
                  <p-tag
                    severity="warning"
                    icon="pi pi-clock"
                    [value]="statusLabel"
                  ></p-tag>
                  <span aria-live="polite">
                    ההגשה ממתינה לבדיקה אוטומטית — העמוד מתעדכן מעצמו.
                  </span>
                </ng-container>

                <ng-container *ngSwitchCase="'ProcessingAi'">
                  <p-tag
                    severity="info"
                    icon="pi pi-spinner pi-spin"
                    [value]="statusLabel"
                  ></p-tag>
                  <span aria-live="polite">
                    הקוד נבדק כעת — העמוד מתעדכן מעצמו.
                  </span>
                </ng-container>

                <!-- נבדק -->
                <ng-container *ngSwitchCase="'Done'">
                  <p-tag
                    severity="success"
                    icon="pi pi-check-circle"
                    [value]="statusLabel"
                  ></p-tag>
                  <app-submission-feedback-panel
                    [submission]="submission"
                  ></app-submission-feedback-panel>

                  <!-- הגשה שנבדקה ועדיין פתוחה — כלומר הציון מתחת לסף התרגיל, או שהמורה
                       אישרה ניסיון נוסף. ⚠️ הכלל מגיע מהשרת (canResubmit) ולא מחושב כאן:
                       הוא מערב את סף הציון של התרגיל ואת נעילת השיעור. -->
                  <span *ngIf="submission.canResubmit">{{ improvableNote }}</span>
                  <ng-container *ngTemplateOutlet="fixAndResubmit"></ng-container>
                </ng-container>

                <!-- דרישה חוסמת שלא התקיימה. Judge0 לא רץ בכלל, ולכן אין כאן תוצאות
                     בדיקות — רק טבלת הדרישות וההסבר. -->
                <ng-container *ngSwitchCase="'RequirementsNotMet'">
                  <p-tag
                    severity="danger"
                    icon="pi pi-ban"
                    [value]="statusLabel"
                  ></p-tag>
                  <span>{{ requirementsNotMetNote }}</span>
                  <app-submission-feedback-panel
                    [submission]="submission"
                  ></app-submission-feedback-panel>
                  <ng-container *ngTemplateOutlet="fixAndResubmit"></ng-container>
                </ng-container>

                <!-- שגיאת קומפילציה -->
                <ng-container *ngSwitchCase="'CompilationFailed'">
                  <p-tag
                    severity="danger"
                    icon="pi pi-times-circle"
                    [value]="statusLabel"
                  ></p-tag>
                  <div *ngIf="submission.compileError">
                    <strong>פלט הקומפיילר:</strong>
                    <pre class="sg-code-box">{{ submission.compileError }}</pre>
                  </div>
                  <span>{{ compilationFailedNote }}</span>
                  <ng-container *ngTemplateOutlet="fixAndResubmit"></ng-container>
                </ng-container>

                <!-- שגיאת בדיקה -->
                <ng-container *ngSwitchCase="'AiFailed'">
                  <p-tag
                    severity="danger"
                    icon="pi pi-exclamation-triangle"
                    [value]="statusLabel"
                  ></p-tag>
                  <div *ngIf="submission.aiError">
                    <strong>פרטי השגיאה:</strong>
                    <pre class="sg-code-box">{{ submission.aiError }}</pre>
                  </div>
                  <span>{{ aiFailedNote }}</span>
                  <ng-container *ngTemplateOutlet="fixAndResubmit"></ng-container>
                </ng-container>

                <!-- תקלת מערכת הבדיקה — לא קשורה לקוד של התלמיד -->
                <ng-container *ngSwitchCase="'JudgeUnavailable'">
                  <p-tag
                    severity="warning"
                    icon="pi pi-exclamation-circle"
                    [value]="statusLabel"
                  ></p-tag>
                  <span>{{ judgeUnavailableNote }}</span>
                </ng-container>

                <ng-container *ngSwitchDefault>
                  <p-tag
                    severity="warning"
                    icon="pi pi-clock"
                    [value]="statusLabel"
                  ></p-tag>
                </ng-container>
              </div>
            </div>

            <!-- Submitted code -->
            <div class="col-12">
              <div class="sg-label">הקוד שהוגש</div>
              <pre class="sg-code-box">{{ submission.sourceCode }}</pre>
            </div>
          </div>
        </p-card>
      </div>
    </section>

    <!-- הכשל הוא בקוד של התלמידה, ולכן יש לה מה לתקן.
         ⚠️ ההחלטה מרוכזת כאן ולא בכל ענף סטטוס בנפרד: canResubmit מגיע מהשרת אחרי
         שהנעילה כבר הוחלה עליו (SubmissionLock.ApplyAsync), וכל ענף שהציג את הכפתור
         ללא תנאי הבטיח לתלמידה תיקון ששיעור שסוכם יסרב לו בלחיצה. -->
    <ng-template #fixAndResubmit>
      <div class="mt-2" *ngIf="submission?.canResubmit">
        <p-button
          label="תיקון והגשה מחדש"
          icon="pi pi-pencil"
          styleClass="sg-btn-primary"
          [routerLink]="['/my', 'submissions', submissionId, 'edit']"
        ></p-button>
      </div>

      <!-- בלי המשפט הזה מסך של שיעור שסוכם היה שותק לגמרי: לא כפתור, לא הסבר. -->
      <span *ngIf="submission?.lockReason">{{ submission!.lockReason }}</span>
    </ng-template>
  `,
  styles: [
    `
      .sg-big-score {
        font-size: var(--text-xl);
        font-weight: 800;
        color: var(--accent);
      }

      .sg-extra-attempt {
        display: flex;
        align-items: center;
        gap: var(--space-2);
        padding: var(--space-2) var(--space-3);
        border-radius: var(--radius-md);
        background: var(--status-info-bg);
        color: var(--status-info-ink);
        font-size: var(--text-sm);
      }
    `,
  ],
})
export class MyFeedbackComponent implements OnInit, OnDestroy {
  submission: SubmissionResponseDto | null = null;
  lessonId: number | null = null;
  loading = false;

  // ⚠️ אלה החליפו את "אין צורך לעשות דבר — צוות ההוראה מטפל בהגשות שנכשלו.": בשני המצבים
  // האלה יש לתלמידה מה לתקן, והשרת תמיד אפשר לה להגיש מחדש — רק הכפתור היה חסר.
  readonly compilationFailedNote =
    "הקוד לא הצליח להתקמפל, ולכן לא נבדק. אפשר לתקן את השגיאה ולהגיש מחדש.";
  readonly aiFailedNote =
    "הבדיקה לא הושלמה. אפשר לתקן את הקוד ולהגיש מחדש.";
  // ⚠️ הניסוח הזה הוא הלב: זו דחייה ולא ציון נמוך. "אם התרגיל דרש רקורסיה וכתבת לולאה —
  // זה כאילו לא עשית". פנייה בלשון נקבה, בית ספר לבנות.
  readonly requirementsNotMetNote =
    "התרגיל דרש דרך פתרון מסוימת, והקוד שלך לא עמד בה — ולכן הוא לא נבדק מול מקרי הבדיקה ולא ניתן ציון. הפרטים בטבלה למטה. תקני והגישי שוב.";
  readonly improvableNote =
    "הציון עדיין מתחת לסף של התרגיל, ולכן ההגשה פתוחה: אפשר לתקן ולהגיש שוב, בלי הגבלת ניסיונות. הציון של הניסיון האחרון הוא שנחשב.";
  // תקלת תשתית — לא באשמת התלמידה ואין לה מה לתקן, ולכן אין כאן כפתור הגשה מחדש.
  readonly judgeUnavailableNote =
    "אירעה תקלה זמנית במערכת הבדיקה — הקוד שלך לא נבדק, וזו לא בעיה בקוד. צוות ההוראה מטפל בכך.";

  submissionId!: number;
  private pollHandle: ReturnType<typeof setInterval> | null = null;

  get statusLabel(): string {
    const status = this.submission?.status;
    return (status && STATUS_LABELS_HE[status]) || "ממתין לבדיקה";
  }

  constructor(
    private route: ActivatedRoute,
    private submissionsService: SubmissionsService,
    private auth: AuthService,
  ) {}

  ngOnInit(): void {
    this.submissionId = Number(
      this.route.snapshot.paramMap.get("submissionId"),
    );
    const lessonIdParam = this.route.snapshot.queryParamMap.get("lessonId");
    this.lessonId = lessonIdParam !== null ? Number(lessonIdParam) : null;
    this.load();
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  private load(): void {
    const studentId = this.auth.studentId();
    if (studentId === null) return;

    this.loading = true;
    this.submissionsService.getById(studentId, this.submissionId).subscribe({
      next: (submission: SubmissionResponseDto) => {
        this.submission = submission;
        this.loading = false;
        this.syncPolling(submission.status);
      },
      error: () => {
        this.loading = false;
      },
    });
  }

  private syncPolling(status: string | null): void {
    const shouldPoll = status === "PendingAi" || status === "ProcessingAi";
    if (shouldPoll && !this.pollHandle) {
      this.pollHandle = setInterval(() => this.refreshSilently(), 5000);
    } else if (!shouldPoll) {
      this.stopPolling();
    }
  }

  private refreshSilently(): void {
    const studentId = this.auth.studentId();
    if (studentId === null) return;

    this.submissionsService.getById(studentId, this.submissionId).subscribe({
      next: (submission: SubmissionResponseDto) => {
        this.submission = submission;
        this.syncPolling(submission.status);
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
  }
}
