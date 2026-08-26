import { CommonModule } from "@angular/common";
import { Component, OnInit, inject } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";

import { BadgeModule } from "primeng/badge";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { ChipModule } from "primeng/chip";
import { ConfirmDialogModule } from "primeng/confirmdialog";
import { DataViewModule } from "primeng/dataview";
import { DropdownModule } from "primeng/dropdown";
import { InputTextModule } from "primeng/inputtext";
import { Menu, MenuModule } from "primeng/menu";
import { TableModule } from "primeng/table";
import { TagModule } from "primeng/tag";
import { TooltipModule } from "primeng/tooltip";

import {
    STATUS_LABELS_HE,
    SubmissionResponseDto,
    SubmissionStatus,
} from "@models/submission.model";
import { SubmissionsService } from "@services/submissions.service";
import { ConfirmationService, MenuItem, MessageService } from "primeng/api";

@Component({
  selector: "app-submissions-list",
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    DataViewModule,
    ButtonModule,
    CardModule,
    TagModule,
    ChipModule,
    TooltipModule,
    BadgeModule,
    ConfirmDialogModule,
    MenuModule,
    DropdownModule,
    InputTextModule,
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

  submissions: SubmissionResponseDto[] = [];
  loading = false;
  studentId!: number;

  query = "";
  statusFilter: SubmissionStatus | null = null;

  readonly statusOptions: { label: string; value: SubmissionStatus | null }[] =
    [
      { label: "כל הסטטוסים", value: null },
      ...(Object.keys(STATUS_LABELS_HE) as SubmissionStatus[]).map(
        (status) => ({ label: STATUS_LABELS_HE[status], value: status }),
      ),
    ];

  // Multi-select (design only — no real bulk delete)
  selectedSubmissions: SubmissionResponseDto[] = [];

  rowMenuItems: MenuItem[] = [];

  get filteredSubmissions(): SubmissionResponseDto[] {
    const q = this.query.trim().toLowerCase();
    return this.submissions.filter(
      (s) =>
        (!q || (s.assignmentName ?? "").toLowerCase().includes(q)) &&
        (!this.statusFilter || s.status === this.statusFilter),
    );
  }

  get hasActiveFilters(): boolean {
    return !!this.query.trim() || !!this.statusFilter;
  }

  clearFilters(): void {
    this.query = "";
    this.statusFilter = null;
  }

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
        this.submissions = data ?? [];
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
    // ⚠️ מפורש ולא לפי תת-מחרוזת: "RequirementsNotMet" אינו מכיל fail/error, והתאמת
    // התת-מחרוזות הייתה מציגה דחייה כתגית מידע ניטרלית.
    if (status === "RequirementsNotMet") return "danger";

    const s = (status ?? "").toLowerCase();
    if (s.includes("pass") || s.includes("success") || s.includes("done"))
      return "success";
    if (s.includes("run") || s.includes("progress")) return "info";
    if (s.includes("warn") || s.includes("pending")) return "warning";
    if (s.includes("fail") || s.includes("error")) return "danger";
    return "info";
  }

  statusIcon(status?: string | null): string {
    // דחייה על דרישה חוסמת — לא תקלה טכנית, ולכן אייקון אחר
    if (status === "RequirementsNotMet") return "pi pi-ban";

    switch (this.statusSeverity(status)) {
      case "success":
        return "pi pi-check-circle";
      case "warning":
        return "pi pi-clock";
      case "danger":
        return "pi pi-times-circle";
      default:
        return "pi pi-info-circle";
    }
  }

  openRowMenu(event: Event, menu: Menu, submission: SubmissionResponseDto): void {
    this.rowMenuItems = [
      {
        label: "עריכה",
        icon: "pi pi-pencil",
        command: () => this.navigateToEdit(submission.id),
      },
      {
        label: "מחיקה",
        icon: "pi pi-trash",
        styleClass: "sg-menu-danger",
        command: () => this.confirmDelete(submission),
      },
    ];
    menu.toggle(event);
  }

  bulkDeleteComingSoon(): void {
    this.messageService.add({
      severity: "info",
      summary: "בקרוב",
      detail: "מחיקה מרובה תהיה זמינה בקרוב",
    });
  }

  clearSelection(): void {
    this.selectedSubmissions = [];
  }

  getStatusLabel(status?: string | null): string {
    if (!status) return "לא ידוע";
    return STATUS_LABELS_HE[status] ?? status;
  }

  confirmDelete(submission: SubmissionResponseDto): void {
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
}
