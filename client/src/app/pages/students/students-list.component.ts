import { CommonModule } from "@angular/common";
import { Component, OnInit, inject } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { Router } from "@angular/router";

import { ConfirmationService, MenuItem, MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { ChipModule } from "primeng/chip";
import { ConfirmDialogModule } from "primeng/confirmdialog";
import { DialogModule } from "primeng/dialog";
import { DropdownModule } from "primeng/dropdown";
import { InputTextModule } from "primeng/inputtext";
import { Menu, MenuModule } from "primeng/menu";
import { TableModule } from "primeng/table";
import { TagModule } from "primeng/tag";
import { ToggleButtonModule } from "primeng/togglebutton";
import { TooltipModule } from "primeng/tooltip";

import { SchoolClassResponseDto } from "@models/class.model";
import {
  ImportStudentsResultDto,
  StudentResponseDto,
} from "@models/student.model";
import { StudentGradesSummaryDto } from "@models/lesson-result.model";
import { ClassesService } from "@services/classes.service";
import { LessonResultsService } from "@services/lesson-results.service";
import { StudentsService } from "@services/students.service";
import { downloadBlob } from "../../core/utils/download";

@Component({
  selector: "app-students-list",
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    CardModule,
    ConfirmDialogModule,
    DialogModule,
    InputTextModule,
    DropdownModule,
    TagModule,
    ChipModule,
    MenuModule,
    ToggleButtonModule,
    TooltipModule,
  ],
  providers: [ConfirmationService, MessageService],
  templateUrl: "./students-list.component.html",
  styleUrls: ["./students-list.component.css"],
})
export class StudentsListComponent implements OnInit {
  private readonly studentsService = inject(StudentsService);
  private readonly classesService = inject(ClassesService);
  private readonly lessonResultsService = inject(LessonResultsService);
  private readonly router = inject(Router);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);

  students: StudentResponseDto[] = [];
  classes: SchoolClassResponseDto[] = [];
  loading = false;

  // Filters
  query = "";
  classFilter: number | null = null;
  filtersOpen = false;
  includeArchived = false;

  // Multi-select (design only — no real bulk delete)
  selectedStudents: StudentResponseDto[] = [];

  rowMenuItems: MenuItem[] = [];

  // Excel export/import
  exporting = false;
  importDialogOpen = false;
  importing = false;
  importFile: File | null = null;
  importResult: ImportStudentsResultDto | null = null;

  // Student grades summary (row click)
  summaryDialogOpen = false;
  summaryLoading = false;
  studentSummary: StudentGradesSummaryDto | null = null;

  get classOptions() {
    return [
      { label: "כל הכיתות", value: null as number | null },
      ...this.classes.map((c) => ({
        label: c.isArchived
          ? `${c.name} — ${c.academicYearHebrew} (ארכיון)`
          : `${c.name} — ${c.academicYearHebrew}`,
        value: c.id as number | null,
      })),
    ];
  }

  get classFilterLabel(): string {
    const cls = this.classes.find((c) => c.id === this.classFilter);
    return cls?.name ?? "";
  }

  get filteredStudents(): StudentResponseDto[] {
    const q = this.query.trim().toLowerCase();
    return this.students.filter(
      (s) =>
        (!q || (s.fullName ?? "").toLowerCase().includes(q)) &&
        (!this.classFilter || s.classId === this.classFilter),
    );
  }

  get hasActiveFilters(): boolean {
    return !!this.query.trim() || !!this.classFilter;
  }

  ngOnInit(): void {
    this.loadStudents();
    this.loadClasses();
  }

  loadClasses(): void {
    this.classesService.getAll(true).subscribe({
      next: (data) => (this.classes = data),
      error: () => {
        // סינון לפי כיתה פשוט לא יוצג — הרשימה עצמה עדיין עובדת
      },
    });
  }

  loadStudents(): void {
    this.loading = true;

    this.studentsService.getAll(this.includeArchived).subscribe({
      next: (data) => {
        this.students = data;
        this.loading = false;
      },
      error: () => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "טעינת הסטודנטים נכשלה",
        });
        this.loading = false;
      },
    });
  }

  toggleFilters(): void {
    this.filtersOpen = !this.filtersOpen;
  }

  openRowMenu(event: Event, menu: Menu, student: StudentResponseDto): void {
    this.rowMenuItems = [
      {
        label: "עריכה",
        icon: "pi pi-pencil",
        command: () => this.navigateToEdit(student.id),
      },
      {
        label: "מחיקה",
        icon: "pi pi-trash",
        styleClass: "sg-menu-danger",
        command: () => this.confirmDelete(student),
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
    this.selectedStudents = [];
  }

  navigateToCreate(): void {
    this.router.navigate(["/students/new"]);
  }

  navigateToEdit(id: number): void {
    this.router.navigate(["/students", id, "edit"]);
  }

  viewSubmissions(studentId: number): void {
    this.router.navigate(["/students", studentId, "submissions"]);
  }

  confirmDelete(student: StudentResponseDto): void {
    this.confirmationService.confirm({
      // מחיקה של תלמידה שיש לה הגשות או ציונים נחסמת בשרת, עם הודעה שאומרת כמה — ולכן
      // כאן רק מציינים שגם חשבון ההתחברות נמחק.
      message: `האם למחוק את "${student.fullName ?? ""}"? חשבון ההתחברות שלה יימחק גם הוא. לא ניתן לשחזר פעולה זו.`,
      header: "אישור מחיקה",
      icon: "pi pi-exclamation-triangle",
      acceptLabel: "מחיקה",
      rejectLabel: "ביטול",
      accept: () => this.deleteStudent(student.id),
    });
  }

  deleteStudent(id: number): void {
    this.studentsService.delete(id).subscribe({
      next: () => {
        this.messageService.add({
          severity: "success",
          summary: "בוצע",
          detail: "הסטודנט/ית נמחק/ה בהצלחה",
        });
        this.loadStudents();
      },
      error: () => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "מחיקת הסטודנט/ית נכשלה",
        });
      },
    });
  }

  resetFilters(): void {
    this.query = "";
    this.classFilter = null;
  }

  exportExcel(): void {
    this.exporting = true;
    this.studentsService.exportExcel().subscribe({
      next: (blob) => {
        downloadBlob(blob, "students.xlsx");
        this.exporting = false;
        this.messageService.add({
          severity: "success",
          summary: "בוצע",
          detail: "הקובץ ירד בהצלחה",
        });
      },
      error: () => {
        this.exporting = false;
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "הייצוא נכשל",
        });
      },
    });
  }

  openImportDialog(): void {
    this.importFile = null;
    this.importResult = null;
    this.importDialogOpen = true;
  }

  onImportFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.importFile = input.files?.[0] ?? null;
    this.importResult = null;
  }

  uploadImport(): void {
    if (!this.importFile) {
      return;
    }

    this.importing = true;
    this.studentsService.importExcel(this.importFile).subscribe({
      next: (result) => {
        this.importing = false;
        this.importResult = result;
        if (result.createdCount > 0) {
          this.messageService.add({
            severity: "success",
            summary: "בוצע",
            detail: `נוספו ${result.createdCount} רשומות בהצלחה`,
          });
          this.loadStudents();
        }
        if (result.errors.length === 0 && result.createdCount > 0) {
          this.importDialogOpen = false;
        }
      },
      error: () => {
        this.importing = false;
      },
    });
  }

  openStudentSummary(student: StudentResponseDto): void {
    this.studentSummary = null;
    this.summaryDialogOpen = true;
    this.summaryLoading = true;

    this.lessonResultsService.getStudentSummary(student.id).subscribe({
      next: (summary) => {
        this.studentSummary = summary;
        this.summaryLoading = false;
      },
      error: () => {
        this.summaryLoading = false;
        this.summaryDialogOpen = false;
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "טעינת ציוני התלמיד/ה נכשלה",
        });
      },
    });
  }
}
