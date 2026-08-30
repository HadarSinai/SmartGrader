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
    SubmissionStatusSeverity,
    statusPresentation,
} from "@models/submission.model";
import { BulkDeleteFailureRow } from "@models/bulk-delete.model";
import { SubmissionsService } from "@services/submissions.service";
import { BulkDeleteResultComponent } from "../../components/bulk-delete-result/bulk-delete-result.component";
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
    BulkDeleteResultComponent,
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

  // ⚠️ מיפוי משותף אחד (STATUS_PRESENTATION), ולא נגזרת מקומית. כאן ישבה גזירה לפי התאמת
  // תת-מחרוזת, ו-"judgeunavailable" לא הכיל אף אחת מהן — כך שתקלת תשתית הוצגה כתגית מידע
  // ניטרלית במסך הזה בלבד, בעוד שבכל מסך אחר היא ענבר.
  statusSeverity(status?: string | null): SubmissionStatusSeverity {
    return statusPresentation(status).severity;
  }

  statusIcon(status?: string | null): string {
    return statusPresentation(status).icon;
  }

  statusLabel(status?: string | null): string {
    return statusPresentation(status).label;
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

  // ── מחיקה מרובה ──────────────────────────────────────────────────────────
  //
  // 🔴 דווקא כאן רוב השורות ייענו בסירוב, וזו התנהגות נכונה: הגשה שנבדקה נושאת ציון
  // ואינה נמחקת, והגשה שהבדיקה עליה פועלת אינה נמחקת גם היא (B-53). זה המסך שבו
  // הצגת הסיבות היא כל ההבדל בין תשובה למסך שנראה תקוע.

  bulkDeleting = false;
  bulkResultOpen = false;
  bulkDeletedCount = 0;
  bulkFailures: BulkDeleteFailureRow[] = [];

  confirmBulkDelete(): void {
    const count = this.selectedSubmissions.length;
    if (count === 0) return;

    this.confirmationService.confirm({
      message: `האם למחוק ${count === 1 ? "הגשה אחת" : count + " הגשות"}? הגשה שכבר נבדקה וקיבלה ציון, או שהבדיקה עליה פועלת, לא תימחק — ותוצג הסיבה. לא ניתן לשחזר פעולה זו.`,
      header: "אישור מחיקה",
      icon: "pi pi-exclamation-triangle",
      acceptLabel: "מחיקה",
      rejectLabel: "ביטול",
      accept: () => this.bulkDelete(),
    });
  }

  private bulkDelete(): void {
    const selected = [...this.selectedSubmissions];
    this.bulkDeleting = true;

    this.submissionsService
      .bulkDelete(
        this.studentId,
        selected.map((s) => s.id),
      )
      .subscribe({
        next: (result) => {
          this.bulkDeleting = false;
          this.bulkDeletedCount = result.deletedCount;

          // השם מגיע מהשורות שכבר על המסך — השרת מחזיר מזהה, ומורה אינה קוראת מזהים.
          this.bulkFailures = result.failures.map((f) => ({
            name:
              selected.find((s) => s.id === f.id)?.assignmentName || "הגשה",
            message: f.message,
          }));

          this.clearSelection();
          this.loadSubmissions();

          if (this.bulkFailures.length > 0) {
            this.bulkResultOpen = true;
            return;
          }

          this.messageService.add({
            severity: "success",
            summary: "בוצע",
            detail: `נמחקו ${result.deletedCount === 1 ? "הגשה אחת" : result.deletedCount + " הגשות"}`,
          });
        },
        error: () => {
          this.bulkDeleting = false;
          this.messageService.add({
            severity: "error",
            summary: "שגיאה",
            detail: "מחיקת ההגשות נכשלה",
          });
        },
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
