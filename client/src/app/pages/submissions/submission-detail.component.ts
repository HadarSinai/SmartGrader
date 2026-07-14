import { CommonModule } from "@angular/common";
import { Component, OnDestroy, OnInit, inject } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import {
  STATUS_LABELS_HE,
  SubmissionResponseDto,
} from "@models/submission.model";
import { SubmissionsService } from "@services/submissions.service";
import { MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { TagModule } from "primeng/tag";

@Component({
  selector: "app-submission-detail",
  standalone: true,
  imports: [CommonModule, CardModule, ButtonModule, TagModule],
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
                <p-button
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
                  עדיין לא נבדק
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

                  <div *ngIf="submission.aiError">
                    <strong>שגיאת AI:</strong>
                    <pre>{{ submission.aiError }}</pre>
                  </div>

                  <div *ngIf="submission.comments" class="sg-note-box">
                    {{ submission.comments }}
                  </div>

                  <div
                    *ngIf="
                      submission.status === 'CompilationFailed' ||
                      submission.status === 'AiFailed'
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
    </section>
  `,
  styles: [
    `
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
