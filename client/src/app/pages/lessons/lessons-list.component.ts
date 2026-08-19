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
  template: `
    <section class="sg-page">
      <div class="pt-3 pb-5">
        <p-card styleClass="sg-card">
          <ng-template pTemplate="header">
            <div
              class="flex flex-column md:flex-row md:align-items-end md:justify-content-between gap-3 px-4 pt-4 pb-2"
            >
              <div class="sg-title">
                <div class="sg-h1">שיעורים</div>
                <div class="sg-h2">ניהול שיעורים ותוכן לימודי</div>
              </div>

              <div class="flex gap-2">
                <p-button
                  label="דוח ציונים"
                  icon="pi pi-file-excel"
                  [outlined]="true"
                  styleClass="sg-btn-secondary"
                  (onClick)="openReportDialog()"
                ></p-button>
                <p-button
                  label="שיעור חדש"
                  icon="pi pi-plus"
                  styleClass="sg-btn-primary"
                  (onClick)="navigateToCreate()"
                ></p-button>
              </div>
            </div>

            <div
              class="flex flex-column md:flex-row md:align-items-center gap-3 px-4 pb-3"
            >
              <span class="p-input-icon-right sg-search">
                <i class="pi pi-search" aria-hidden="true"></i>
                <input
                  pInputText
                  type="text"
                  [(ngModel)]="query"
                  placeholder="חיפוש לפי מקצוע או נושא..."
                  aria-label="חיפוש שיעורים"
                />
              </span>

              <p-dropdown
                [options]="classOptions"
                [(ngModel)]="classFilter"
                optionLabel="label"
                optionValue="value"
                placeholder="כל הכיתות"
                [showClear]="true"
                aria-label="סינון לפי כיתה"
              ></p-dropdown>

              <div class="sg-active-filters" aria-label="מסננים פעילים">
                <p-chip
                  *ngIf="query.trim()"
                  label="מקצוע/נושא: {{ query.trim() }}"
                  [removable]="true"
                  (onRemove)="query = ''"
                >
                </p-chip>
                <p-chip
                  *ngIf="classFilter"
                  label="כיתה: {{ classFilterLabel }}"
                  [removable]="true"
                  (onRemove)="classFilter = null"
                ></p-chip>
              </div>
            </div>
          </ng-template>

          <!-- Selection toolbar (design only — bulk delete is a future task) -->
          <div
            class="sg-selection-bar"
            *ngIf="selectedLessons.length > 0"
            aria-live="polite"
          >
            <span>נבחרו {{ selectedLessons.length }}</span>
            <p-button
              label="מחיקת נבחרים"
              icon="pi pi-trash"
              severity="danger"
              [text]="true"
              (onClick)="bulkDeleteComingSoon()"
            ></p-button>
            <p-button
              label="ביטול בחירה"
              [text]="true"
              (onClick)="clearSelection()"
            ></p-button>
          </div>

          <!-- Desktop table -->
          <div class="sg-table-wrap desktop-only">
            <p-table
              [value]="filteredLessons"
              [loading]="loading"
              [paginator]="true"
              [rows]="10"
              [rowsPerPageOptions]="[10, 25, 50]"
              dataKey="id"
              [(selection)]="selectedLessons"
              responsiveLayout="scroll"
              styleClass="sg-table"
            >
              <ng-template pTemplate="header">
                <tr>
                  <th style="width: 3rem">
                    <p-tableHeaderCheckbox
                      aria-label="בחירת כל השורות"
                    ></p-tableHeaderCheckbox>
                  </th>
                  <th pSortableColumn="courseName">
                    מקצוע <p-sortIcon field="courseName"></p-sortIcon>
                  </th>
                  <th pSortableColumn="subject">
                    נושא <p-sortIcon field="subject"></p-sortIcon>
                  </th>
                  <th class="text-center">כיתות</th>
                  <th class="text-center" pSortableColumn="lessonDate">
                    תאריך <p-sortIcon field="lessonDate"></p-sortIcon>
                  </th>
                  <th class="text-center">תרגילים</th>
                  <th class="text-center" style="width: 5rem">תוצאות</th>
                  <th class="text-center" style="width: 5rem">פעולות</th>
                </tr>
              </ng-template>

              <ng-template pTemplate="body" let-lesson>
                <tr>
                  <td>
                    <p-tableCheckbox
                      [value]="lesson"
                      [attr.aria-label]="'בחירת ' + lesson.courseName"
                    ></p-tableCheckbox>
                  </td>
                  <td class="font-bold text-color">{{ lesson.courseName }}</td>
                  <td>{{ lesson.subject || "—" }}</td>
                  <td class="text-center">{{ lesson.classNames || "—" }}</td>
                  <td class="text-center">
                    {{ lesson.lessonDateHebrew }} ({{
                      lesson.lessonDate | date: "dd.MM.yy"
                    }})
                  </td>
                  <td class="text-center">
                    <p-button
                      [label]="(lesson.assignmentsCount ?? 0) + ' תרגילים'"
                      icon="pi pi-file"
                      [outlined]="true"
                      styleClass="sg-btn-secondary"
                      pTooltip="מעבר לתרגילי השיעור"
                      tooltipPosition="top"
                      [attr.aria-label]="
                        'תרגילי השיעור: ' + lesson.courseName
                      "
                      (onClick)="viewAssignments(lesson.id)"
                    >
                    </p-button>
                  </td>
                  <td class="text-center">
                    <p-button
                      icon="pi pi-chart-bar"
                      [text]="true"
                      pTooltip="תוצאות השיעור"
                      tooltipPosition="top"
                      [attr.aria-label]="'תוצאות שיעור: ' + lesson.courseName"
                      (onClick)="viewResults(lesson.id)"
                    ></p-button>
                  </td>
                  <td class="text-center">
                    <p-button
                      icon="pi pi-ellipsis-h"
                      [text]="true"
                      [attr.aria-label]="
                        'פעולות נוספות: ' + lesson.courseName
                      "
                      (onClick)="openRowMenu($event, rowMenu, lesson)"
                    ></p-button>
                  </td>
                </tr>
              </ng-template>

              <ng-template pTemplate="emptymessage">
                <tr>
                  <td
                    colspan="8"
                    class="text-center px-3 py-6 text-color-secondary"
                  >
                    <div
                      class="flex flex-column align-items-center justify-content-center gap-3"
                    >
                      <i class="pi pi-inbox text-4xl" aria-hidden="true"></i>
                      <div>אין שיעורים להצגה.</div>
                      <p-button
                        label="שיעור חדש"
                        icon="pi pi-plus"
                        styleClass="sg-btn-primary"
                        (onClick)="navigateToCreate()"
                      ></p-button>
                    </div>
                  </td>
                </tr>
              </ng-template>
            </p-table>
          </div>

          <!-- Mobile cards -->
          <div class="mobile-only px-3 pb-3">
            <p-dataView
              [value]="filteredLessons"
              [loading]="loading"
              layout="list"
            >
              <ng-template pTemplate="list" let-items>
                <div class="card-list">
                  <div *ngFor="let item of items" class="mobile-card">
                    <div class="mobile-card__header">
                      <div class="mobile-card__title">
                        {{ item.courseName }}
                      </div>
                      <div class="mobile-card__subtitle">
                        {{ item.subject || "—" }}
                      </div>
                    </div>

                    <div class="mobile-card__meta">
                      <div>
                        <span class="label">תאריך</span>
                        {{ item.lessonDateHebrew }} ({{
                          item.lessonDate | date: "dd.MM.yy"
                        }})
                      </div>
                      <div>
                        <span class="label">תרגילים</span>
                        {{ item.assignmentsCount ?? 0 }}
                      </div>
                      <div>
                        <span class="label">כיתות</span>
                        {{ item.classNames || "—" }}
                      </div>
                    </div>

                    <div class="mobile-card__actions">
                      <p-button
                        label="תרגילים"
                        icon="pi pi-file"
                        [outlined]="true"
                        (onClick)="viewAssignments(item.id)"
                      ></p-button>
                      <p-button
                        label="תוצאות"
                        icon="pi pi-chart-bar"
                        [outlined]="true"
                        (onClick)="viewResults(item.id)"
                      ></p-button>
                      <p-button
                        icon="pi pi-ellipsis-h"
                        [text]="true"
                        [attr.aria-label]="
                          'פעולות נוספות: ' + item.courseName
                        "
                        (onClick)="openRowMenu($event, rowMenu, item)"
                      ></p-button>
                    </div>
                  </div>
                </div>
              </ng-template>
            </p-dataView>
          </div>
        </p-card>
      </div>
    </section>

    <p-menu
      #rowMenu
      [popup]="true"
      appendTo="body"
      [model]="rowMenuItems"
      styleClass="sg-row-menu"
    ></p-menu>

    <p-confirmDialog></p-confirmDialog>

    <p-dialog
      header="דוח ציונים תקופתי"
      [(visible)]="reportDialogVisible"
      [modal]="true"
      [style]="{ width: '28rem' }"
      [dismissableMask]="true"
    >
      <div class="flex flex-column gap-4" dir="rtl">
        <div class="flex flex-column gap-2">
          <label>מתאריך</label>
          <app-hebrew-date-picker
            [(ngModel)]="reportFrom"
            [ngModelOptions]="{ standalone: true }"
          ></app-hebrew-date-picker>
        </div>
        <div class="flex flex-column gap-2">
          <label>עד תאריך</label>
          <app-hebrew-date-picker
            [(ngModel)]="reportTo"
            [ngModelOptions]="{ standalone: true }"
          ></app-hebrew-date-picker>
        </div>
      </div>

      <ng-template pTemplate="footer">
        <p-button
          label="ביטול"
          [text]="true"
          (onClick)="closeReportDialog()"
        ></p-button>
        <p-button
          label="ייצוא"
          icon="pi pi-download"
          styleClass="sg-btn-primary"
          [disabled]="!reportFrom || !reportTo"
          [loading]="exportingReport"
          (onClick)="exportReport()"
        ></p-button>
      </ng-template>
    </p-dialog>
  `,
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
    this.rowMenuItems = [
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
