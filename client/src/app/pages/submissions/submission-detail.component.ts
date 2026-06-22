import { CommonModule } from "@angular/common";
import { Component, OnInit, inject } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { SubmissionResponseDto } from "@models/submission.model";
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
                <div class="sg-h1">פרטי הגשה</div>
                <div class="sg-h2">צפייה בפרטים, סטטוס וקוד</div>
              </div>

              <div class="flex align-items-center gap-2 flex-wrap">
                <p-button
                  label="חזרה"
                  icon="pi pi-arrow-left"
                  severity="secondary"
                  [outlined]="true"
                  (onClick)="navigateToList()"
                >
                </p-button>
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
                  {{ submission.submittedAt | date: "medium" }}
                </div>
              </div>

              <div class="col-12 md:col-6">
                <div class="text-xs font-bold text-color-secondary mb-1">
                  סטטוס
                </div>
                <p-tag
                  [value]="submission.status || 'Unknown'"
                  [severity]="getStatusSeverity(submission.status)"
                >
                </p-tag>
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

              <div class="col-12" *ngIf="submission.comments">
                <div class="text-xs font-bold text-color-secondary mb-2">
                  הערות
                </div>
                <div class="sg-note-box">{{ submission.comments }}</div>
              </div>

              <div class="col-12" *ngIf="submission.aiError">
                <div class="text-xs font-bold text-color-secondary mb-2">
                  שגיאת AI
                </div>
                <div class="sg-warn-box">{{ submission.aiError }}</div>
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
  styles: [],
})
export class SubmissionDetailComponent implements OnInit {
  private readonly submissionsService = inject(SubmissionsService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly messageService = inject(MessageService);

  submission: SubmissionResponseDto | null = null;
  loading = false;
  studentId!: number;
  submissionId!: number;

  ngOnInit(): void {
    const studentIdParam = this.route.snapshot.paramMap.get("studentId");
    const submissionIdParam = this.route.snapshot.paramMap.get("submissionId");

    if (studentIdParam && submissionIdParam) {
      this.studentId = parseInt(studentIdParam, 10);
      this.submissionId = parseInt(submissionIdParam, 10);
      this.loadSubmission();
    }
  }

  loadSubmission(): void {
    this.loading = true;
    this.submissionsService
      .getById(this.studentId, this.submissionId)
      .subscribe({
        next: (data: SubmissionResponseDto) => {
          this.submission = data;
          this.loading = false;
        },
        error: (_error: unknown) => {
          this.messageService.add({
            severity: "error",
            summary: "Error",
            detail: "Failed to load submission",
          });
          this.loading = false;
        },
      });
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
