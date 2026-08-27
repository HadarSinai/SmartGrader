import { CommonModule } from "@angular/common";
import { Component, OnDestroy, OnInit, inject } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";
import {
  STATUS_LABELS_HE,
  SUBMISSION_POLL_INTERVAL_MS,
  SubmissionResponseDto,
  SubmissionStatusPresentation,
  statusPresentation,
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
import { SubmittedCodeComponent } from "@components/submitted-code/submitted-code.component";

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
    SubmittedCodeComponent,
  ],
  templateUrl: "./submission-detail.component.html",
  styleUrls: ["./submission-detail.component.css"],
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

  /** המראה של סטטוס — מקור אחד לכל המסכים. */
  statusOf(status?: string | null): SubmissionStatusPresentation {
    return statusPresentation(status);
  }

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
      this.pollHandle = setInterval(
        () => this.refreshSilently(),
        SUBMISSION_POLL_INTERVAL_MS,
      );
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
