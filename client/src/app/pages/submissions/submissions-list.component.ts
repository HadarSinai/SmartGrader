import { CommonModule } from "@angular/common";
import { Component, OnInit, inject } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";

import { BadgeModule } from "primeng/badge";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { ChipModule } from "primeng/chip";
import { ConfirmDialogModule } from "primeng/confirmdialog";
import { DataViewModule } from "primeng/dataview";
import { TableModule } from "primeng/table";
import { TagModule } from "primeng/tag";
import { TooltipModule } from "primeng/tooltip";

import { SubmissionExtended } from "@models/submission-extended.model";
import {
  STATUS_LABELS_HE,
  SubmissionResponseDto,
} from "@models/submission.model";
import { SubmissionsService } from "@services/submissions.service";
import { ConfirmationService, MessageService } from "primeng/api";

@Component({
  selector: "app-submissions-list",
  standalone: true,
  imports: [
    CommonModule,
    TableModule,
    DataViewModule,
    ButtonModule,
    CardModule,
    TagModule,
    ChipModule,
    TooltipModule,
    BadgeModule,
    ConfirmDialogModule,
  ],
  providers: [ConfirmationService],
  templateUrl: "./submissions-list.component.html",
  styleUrls: ["./submissions-list.component.css"],
})
export class SubmissionsListComponent implements OnInit {
  private readonly submissionsService = inject(SubmissionsService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);

  submissions: SubmissionExtended[] = [];
  loading = false;
  studentId!: number;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get("studentId");
    if (!id) {
      this.navigateToStudents();
      return;
    }

    this.studentId = parseInt(id, 10);
    this.loadSubmissions();
  }

  loadSubmissions(): void {
    this.loading = true;
    this.submissionsService.getByStudent(this.studentId).subscribe({
      next: (data: SubmissionResponseDto[]) => {
        this.submissions = (data ?? []).map((s) => this.toExtended(s));
        this.loading = false;
      },
      error: (_err: unknown) => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "טעינת ההגשות נכשלה",
          life: 4500,
        });
        this.loading = false;
      },
    });
  }

  navigateToStudents(): void {
    this.router.navigate(["/students"]);
  }

  navigateToCreate(): void {
    this.router.navigate(["/students", this.studentId, "submissions", "new"]);
  }

  navigateToView(submissionId: number): void {
    this.router.navigate([
      "/students",
      this.studentId,
      "submissions",
      submissionId,
    ]);
  }

  navigateToEdit(submissionId: number): void {
    this.router.navigate([
      "/students",
      this.studentId,
      "submissions",
      submissionId,
      "edit",
    ]);
  }

  statusSeverity(
    status?: string | null,
  ): "success" | "info" | "warning" | "danger" {
    const s = (status ?? "").toLowerCase();
    if (s.includes("pass") || s.includes("success") || s.includes("done"))
      return "success";
    if (s.includes("run") || s.includes("progress")) return "info";
    if (s.includes("warn") || s.includes("pending")) return "warning";
    if (s.includes("fail") || s.includes("error")) return "danger";
    return "info";
  }

  getStatusLabel(status?: string | null): string {
    if (!status) return "לא ידוע";
    return STATUS_LABELS_HE[status] ?? status;
  }

  confirmDelete(submission: SubmissionExtended): void {
    this.confirmationService.confirm({
      message: `האם למחוק את ההגשה עבור "${submission.assignmentName ?? ""}"?  לא ניתן לשחזר פעולה זו.`,
      header: "אישור מחיקה",
      icon: "pi pi-exclamation-triangle",
      acceptLabel: "מחיקה",
      rejectLabel: "ביטול",
      accept: () => this.deleteSubmission(submission.id),
    });
  }

  deleteSubmission(submissionId: number): void {
    this.submissionsService.delete(this.studentId, submissionId).subscribe({
      next: () => {
        this.messageService.add({
          severity: "success",
          summary: "בוצע",
          detail: "ההגשה נמחקה בהצלחה",
        });
        this.loadSubmissions();
      },
      error: () => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "מחיקת ההגשה נכשלה",
        });
      },
    });
  }

  private toExtended(s: SubmissionResponseDto): SubmissionExtended {
    const code = s.sourceCode ?? "";
    const preview = code
      .replace(/\r\n/g, "\n")
      .split("\n")
      .slice(0, 3)
      .join(" \n ")
      .trim();

    return {
      ...s,
      codePreview: preview || "(אין קוד)",
      evaluatedBy: s.comments ? "AI" : "Manual",
    };
  }
}
