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

@Component({
  selector: "app-my-feedback",
  standalone: true,
  imports: [CommonModule, RouterModule, ButtonModule, CardModule, TagModule],
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
            </div>

            <!-- Unified status area -->
            <div class="col-12">
              <div
                class="sg-status-box"
                [class.sg-status-box--error]="
                  submission.status === 'CompilationFailed' ||
                  submission.status === 'AiFailed'
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
                  <div *ngIf="submission.comments" class="sg-note-box">
                    {{ submission.comments }}
                  </div>
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
                  <span>{{ failureNote }}</span>
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
                  <span>{{ failureNote }}</span>
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
  `,
  styles: [
    `
      .sg-big-score {
        font-size: var(--text-xl);
        font-weight: 800;
        color: var(--accent);
      }
    `,
  ],
})
export class MyFeedbackComponent implements OnInit, OnDestroy {
  submission: SubmissionResponseDto | null = null;
  lessonId: number | null = null;
  loading = false;

  readonly failureNote = "אין צורך לעשות דבר — צוות ההוראה מטפל בהגשות שנכשלו.";

  private submissionId!: number;
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
