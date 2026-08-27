import { CommonModule } from "@angular/common";
import { Component, OnInit, inject } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { Router } from "@angular/router";

import { ConfirmationService, MenuItem, MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { ChipModule } from "primeng/chip";
import { ConfirmDialogModule } from "primeng/confirmdialog";
import { DataViewModule } from "primeng/dataview";
import { DialogModule } from "primeng/dialog";
import { DropdownModule } from "primeng/dropdown";
import { InputTextModule } from "primeng/inputtext";
import { Menu, MenuModule } from "primeng/menu";
import { TableModule } from "primeng/table";
import { TooltipModule } from "primeng/tooltip";

import {
  HebrewDatePickerComponent,
  HebrewDateValue,
  getHebrewToday,
} from "@components/hebrew-date-picker/hebrew-date-picker.component";
import { SchoolClassResponseDto } from "@models/class.model";
import { LessonResponseDto } from "@models/lesson.model";
import { ClassesService } from "@services/classes.service";
import { LessonsService } from "@services/lessons.service";
import { LessonResultsService } from "@services/lesson-results.service";
import { downloadBlob } from "../../core/utils/download";

@Component({
  selector: "app-lessons-list",
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    InputTextModule,
    DropdownModule,
    ChipModule,
    TableModule,
    DataViewModule,
    ButtonModule,
    CardModule,
    ConfirmDialogModule,
    DialogModule,
    TooltipModule,
    MenuModule,
    HebrewDatePickerComponent,
  ],
  providers: [ConfirmationService],
  styleUrls: ["./lessons-list.component.css"],
  templateUrl: "./lessons-list.component.html",
})
export class LessonsListComponent implements OnInit {
  private readonly lessonsService = inject(LessonsService);
  private readonly classesService = inject(ClassesService);
  private readonly lessonResultsService = inject(LessonResultsService);
  private readonly router = inject(Router);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);

  lessons: LessonResponseDto[] = [];
  classes: SchoolClassResponseDto[] = [];
  loading = false;

  query = "";
  classFilter: number | null = null;

  reportDialogVisible = false;
  reportFrom: HebrewDateValue | null = null;
  reportTo: HebrewDateValue | null = null;
  exportingReport = false;

  // Multi-select (design only — no real bulk delete)
  selectedLessons: LessonResponseDto[] = [];

  rowMenuItems: MenuItem[] = [];

  get classOptions() {
    return this.classes.map((c) => ({
      label: c.isArchived
        ? `${c.name} — ${c.academicYearHebrew} (ארכיון)`
        : `${c.name} — ${c.academicYearHebrew}`,
      value: c.id,
    }));
  }

  get classFilterLabel(): string {
    return this.classes.find((c) => c.id === this.classFilter)?.name ?? "";
  }

  get filteredLessons(): LessonResponseDto[] {
    const q = this.query.trim().toLowerCase();
    return this.lessons.filter(
      (l) =>
        (!q ||
          l.courseName.toLowerCase().includes(q) ||
          (l.subject ?? "").toLowerCase().includes(q)) &&
        (!this.classFilter ||
          (l.classes ?? []).some((c) => c.id === this.classFilter)),
    );
  }

  ngOnInit(): void {
    this.loadLessons();
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

  loadLessons(): void {
    this.loading = true;
    this.lessonsService.getAll().subscribe({
      next: (data: LessonResponseDto[]) => {
        this.lessons = data;
        this.loading = false;
      },
      error: () => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "טעינת השיעורים נכשלה",
        });
        this.loading = false;
      },
    });
  }

  openRowMenu(event: Event, menu: Menu, lesson: LessonResponseDto): void {
    // ⚠️ «תוצאות» חי כאן ולא בעמודה. עמודה מרוויחה את מקומה כשהיא מוסרת מידע
    // שמשנה החלטה, לא כשהיא חוסכת קליק — ותוצאות השיעור הן יעד, לא סימן.
    // «תרגילים» נשאר עמודה בדיוק מהסיבה ההפוכה: אפס תרגילים אומר שהשיעור
    // עדיין לא מוכן, וזו החלטה שנעשית מהמסך הזה.
    this.rowMenuItems = [
      {
        label: "תוצאות השיעור",
        icon: "pi pi-chart-bar",
        command: () => this.viewResults(lesson.id),
      },
      {
        label: "עריכה",
        icon: "pi pi-pencil",
        command: () => this.navigateToEdit(lesson.id),
      },
      {
        label: "מחיקה",
        icon: "pi pi-trash",
        styleClass: "sg-menu-danger",
        command: () => this.confirmDelete(lesson),
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
    this.selectedLessons = [];
  }

  navigateToCreate(): void {
    this.router.navigate(["/lessons/new"]);
  }

  navigateToEdit(id: number): void {
    this.router.navigate(["/lessons", id, "edit"]);
  }

  viewAssignments(lessonId: number): void {
    this.router.navigate(["/lessons", lessonId, "assignments"]);
  }

  viewResults(lessonId: number): void {
    this.router.navigate(["/lessons", lessonId, "results"]);
  }

  confirmDelete(lesson: LessonResponseDto): void {
    this.confirmationService.confirm({
      // הטקסט אומר במפורש מה נמחק יחד עם השיעור. מחיקה של שיעור שיש בו הגשות או ציונים
      // סופיים נחסמת בשרת, וההודעה משם מציינת כמה יש — ולכן אין כאן אישור שני.
      message: `האם למחוק את השיעור "${lesson.courseName}${
        lesson.subject ? " — " + lesson.subject : ""
      }"? כל התרגילים שלו יימחקו גם הם. לא ניתן לשחזר פעולה זו.`,
      header: "אישור מחיקה",
      icon: "pi pi-exclamation-triangle",
      acceptLabel: "מחיקה",
      rejectLabel: "ביטול",
      accept: () => this.deleteLesson(lesson.id),
    });
  }

  deleteLesson(id: number): void {
    this.lessonsService.delete(id).subscribe({
      next: () => {
        this.messageService.add({
          severity: "success",
          summary: "בוצע",
          detail: "השיעור נמחק בהצלחה",
        });
        this.loadLessons();
      },
      error: () => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "מחיקת השיעור נכשלה",
        });
      },
    });
  }

  openReportDialog(): void {
    const today = getHebrewToday();
    this.reportFrom = today
      ? { hebrewYear: today.hebrewYear, hebrewMonth: 1, hebrewDay: 1 }
      : null;
    this.reportTo = today;
    this.reportDialogVisible = true;
  }

  closeReportDialog(): void {
    this.reportDialogVisible = false;
  }

  exportReport(): void {
    if (!this.reportFrom || !this.reportTo) return;

    this.exportingReport = true;
    this.lessonResultsService
      .exportPeriodReport(this.reportFrom, this.reportTo)
      .subscribe({
        next: (blob) => {
          downloadBlob(blob, "grades-report.xlsx");
          this.exportingReport = false;
          this.reportDialogVisible = false;
          this.messageService.add({
            severity: "success",
            summary: "בוצע",
            detail: "הדוח ירד בהצלחה",
          });
        },
        error: () => {
          this.exportingReport = false;
          this.messageService.add({
            severity: "error",
            summary: "שגיאה",
            detail: "ייצוא הדוח נכשל",
          });
        },
      });
  }
}
