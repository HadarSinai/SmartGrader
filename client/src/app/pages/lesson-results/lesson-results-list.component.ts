import { CommonModule } from "@angular/common";
import { Component, OnInit, inject } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { forkJoin, of } from "rxjs";
import { catchError, map } from "rxjs/operators";

import { MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { CheckboxModule } from "primeng/checkbox";
import { DataViewModule } from "primeng/dataview";
import { DialogModule } from "primeng/dialog";
import { DropdownModule } from "primeng/dropdown";
import { InputNumberModule } from "primeng/inputnumber";
import { InputTextModule } from "primeng/inputtext";
import { TableModule } from "primeng/table";
import { TagModule } from "primeng/tag";
import { TooltipModule } from "primeng/tooltip";

import { FormsModule } from "@angular/forms";
import {
  CompleteLessonRequestDto,
  LessonResultResponseDto,
} from "@models/lesson-result.model";
import { StudentResponseDto } from "@models/student.model";
import { LessonResultsService } from "@services/lesson-results.service";
import { StudentsService } from "@services/students.service";
import { downloadBlob } from "../../core/utils/download";

interface LessonResultRowVm {
  studentId: number;
  studentName: string;
  totalAssignments: number;
  completedAssignments: number;
  finalScore: number | null;
  isComplete: boolean;
}

@Component({
  selector: "app-lesson-results-list",
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    CardModule,
    TableModule,
    DataViewModule,
    TagModule,
    TooltipModule,
    DialogModule,
    InputNumberModule,
    CheckboxModule,
    DropdownModule,
    InputTextModule,
  ],
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
                  (click)="navigateToLessons()"
                  (keydown.enter)="navigateToLessons()"
                >
                  <i class="pi pi-arrow-right" aria-hidden="true"></i>
                  חזרה לשיעורים
                </a>
                <div class="sg-h1">תוצאות שיעור</div>
                <div class="sg-h2">מעקב אחר התקדמות התלמידים בשיעור</div>
              </div>

              <p-button
                label="ייצוא"
                icon="pi pi-download"
                [outlined]="true"
                styleClass="sg-btn-secondary"
                [loading]="exporting"
                (onClick)="exportExcel()"
              ></p-button>
            </div>

            <!-- Search + filters row -->
            <div
              class="flex flex-column md:flex-row md:align-items-center gap-3 px-4 pb-3"
            >
              <span class="p-input-icon-right sg-search">
                <i class="pi pi-search" aria-hidden="true"></i>
                <input
                  pInputText
                  type="text"
                  [(ngModel)]="query"
                  placeholder="חיפוש לפי שם..."
                  aria-label="חיפוש תלמידים"
                />
              </span>

              <p-dropdown
                inputId="completionFilter"
                [options]="completionOptions"
                [(ngModel)]="completionFilter"
                optionLabel="label"
                optionValue="value"
                placeholder="כל הסטטוסים"
                aria-label="סינון לפי סטטוס"
              ></p-dropdown>

              <p-button
                *ngIf="hasActiveFilters"
                label="איפוס"
                [text]="true"
                (onClick)="clearFilters()"
                aria-label="איפוס סינון"
              ></p-button>
            </div>
          </ng-template>

          <!-- Desktop table -->
          <div class="sg-table-wrap desktop-only">
            <p-table
              [value]="filteredRows"
              [loading]="loading"
              responsiveLayout="scroll"
              styleClass="sg-table"
            >
              <ng-template pTemplate="header">
                <tr>
                  <th class="text-center" pSortableColumn="studentName">
                    שם התלמיד/ה
                    <p-sortIcon field="studentName"></p-sortIcon>
                  </th>
                  <th
                    class="text-center"
                    pSortableColumn="completedAssignments"
                  >
                    התקדמות
                    <p-sortIcon field="completedAssignments"></p-sortIcon>
                  </th>
                  <th class="text-center" pSortableColumn="finalScore">
                    ציון סופי <p-sortIcon field="finalScore"></p-sortIcon>
                  </th>
                  <th class="text-center" pSortableColumn="isComplete">
                    סטטוס <p-sortIcon field="isComplete"></p-sortIcon>
                  </th>
                  <th class="text-center">פעולות</th>
                </tr>
              </ng-template>

              <ng-template pTemplate="body" let-row>
                <tr>
                  <td class="font-bold text-color">{{ row.studentName }}</td>
                  <td class="text-center">
                    <span
                      class="font-semibold text-color"
                      [attr.aria-label]="
                        'הושלמו ' +
                        row.completedAssignments +
                        ' מתוך ' +
                        row.totalAssignments +
                        ' תרגילים'
                      "
                    >
                      {{ row.completedAssignments }}/{{ row.totalAssignments }}
                    </span>
                  </td>
                  <td class="text-center">
                    {{ row.finalScore ?? "טרם נקבע" }}
                  </td>
                  <td class="text-center">
                    <p-tag
                      *ngIf="row.isComplete"
                      severity="success"
                      value="הושלם"
                      icon="pi pi-check-circle"
                    ></p-tag>
                    <p-tag
                      *ngIf="!row.isComplete"
                      severity="info"
                      value="בתהליך"
                      icon="pi pi-info-circle"
                    ></p-tag>
                  </td>
                  <td class="text-center">
                    <p-button
                      *ngIf="
                        row.completedAssignments === row.totalAssignments &&
                        !row.isComplete
                      "
                      label="סיום שיעור"
                      icon="pi pi-flag"
                      [text]="true"
                      [attr.aria-label]="'סיום שיעור עבור ' + row.studentName"
                      (onClick)="openFinalize(row)"
                    ></p-button>
                  </td>
                </tr>
              </ng-template>

              <ng-template pTemplate="emptymessage">
                <tr>
                  <td
                    colspan="5"
                    class="text-center px-3 py-6 text-color-secondary"
                  >
                    <ng-container *ngIf="hasActiveFilters; else noStudents">
                      לא נמצאו תוצאות התואמות לסינון.
                      <p-button
                        label="איפוס סינון"
                        [text]="true"
                        (onClick)="clearFilters()"
                      ></p-button>
                    </ng-container>
                    <ng-template #noStudents>אין תלמידים להצגה.</ng-template>
                  </td>
                </tr>
              </ng-template>
            </p-table>
          </div>

          <!-- Mobile cards -->
          <div class="mobile-only px-3 pb-3">
            <p-dataView
              [value]="filteredRows"
              [loading]="loading"
              layout="list"
            >
              <ng-template pTemplate="list" let-items>
                <div class="card-list">
                  <div *ngFor="let item of items" class="mobile-card">
                    <div class="mobile-card__header">
                      <div class="mobile-card__title">
                        {{ item.studentName }}
                      </div>
                      <p-tag
                        *ngIf="item.isComplete"
                        severity="success"
                        value="הושלם"
                        icon="pi pi-check-circle"
                      ></p-tag>
                      <p-tag
                        *ngIf="!item.isComplete"
                        severity="info"
                        value="בתהליך"
                        icon="pi pi-info-circle"
                      ></p-tag>
                    </div>

                    <div class="mobile-card__meta">
                      <div>
                        <span class="label">התקדמות</span>
                        {{ item.completedAssignments }}/{{
                          item.totalAssignments
                        }}
                      </div>
                      <div>
                        <span class="label">ציון סופי</span>
                        {{ item.finalScore ?? "טרם נקבע" }}
                      </div>
                    </div>

                    <div class="mobile-card__actions">
                      <p-button
                        *ngIf="
                          item.completedAssignments === item.totalAssignments &&
                          !item.isComplete
                        "
                        label="סיום שיעור"
                        icon="pi pi-flag"
                        [outlined]="true"
                        [attr.aria-label]="
                          'סיום שיעור עבור ' + item.studentName
                        "
                        (onClick)="openFinalize(item)"
                      ></p-button>
                    </div>
                  </div>
                </div>
              </ng-template>
            </p-dataView>
          </div>
        </p-card>
      </div>

      <!-- Finalize dialog -->
      <p-dialog
        header="סיום שיעור"
        [(visible)]="finalizeDialogOpen"
        [modal]="true"
        [style]="{ width: '24rem' }"
        [draggable]="false"
        [resizable]="false"
      >
        <div class="flex flex-column gap-3" *ngIf="finalizeRow">
          <div>
            קביעת ציון סופי עבור
            <strong>{{ finalizeRow.studentName }}</strong>
          </div>
          <div class="flex align-items-center gap-2">
            <p-checkbox
              inputId="hasBonus"
              [(ngModel)]="hasBonus"
              [binary]="true"
              (onChange)="onBonusChange()"
            ></p-checkbox>
            <label class="sg-label mb-0" for="hasBonus">כולל בונוס</label>
          </div>
          <div>
            <label class="sg-label" for="finalScore"
              >ציון סופי (0–{{ maxScore }}) *</label
            >
            <p-inputNumber
              inputId="finalScore"
              [(ngModel)]="finalScore"
              [min]="0"
              [max]="maxScore"
              [showButtons]="true"
              styleClass="w-full"
            ></p-inputNumber>
          </div>
        </div>
        <ng-template pTemplate="footer">
          <p-button
            label="ביטול"
            severity="secondary"
            [outlined]="true"
            (onClick)="finalizeDialogOpen = false"
          ></p-button>
          <p-button
            label="שמירה"
            styleClass="sg-btn-primary"
            [loading]="finalizeSaving"
            [disabled]="finalScore === null"
            (onClick)="saveFinalize()"
          ></p-button>
        </ng-template>
      </p-dialog>
    </section>
  `,
  styles: [],
})
export class LessonResultsListComponent implements OnInit {
  private readonly lessonResultsService = inject(LessonResultsService);
  private readonly studentsService = inject(StudentsService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly messageService = inject(MessageService);

  lessonId!: number;
  rows: LessonResultRowVm[] = [];
  loading = false;
  exporting = false;

  query = "";
  completionFilter: boolean | null = null;

  readonly completionOptions: { label: string; value: boolean | null }[] = [
    { label: "כל הסטטוסים", value: null },
    { label: "הושלם", value: true },
    { label: "בתהליך", value: false },
  ];

  get filteredRows(): LessonResultRowVm[] {
    const q = this.query.trim().toLowerCase();
    return this.rows.filter(
      (r) =>
        (!q || r.studentName.toLowerCase().includes(q)) &&
        (this.completionFilter === null ||
          r.isComplete === this.completionFilter),
    );
  }

  get hasActiveFilters(): boolean {
    return !!this.query.trim() || this.completionFilter !== null;
  }

  clearFilters(): void {
    this.query = "";
    this.completionFilter = null;
  }

  // Finalize dialog
  finalizeDialogOpen = false;
  finalizeRow: LessonResultRowVm | null = null;
  finalScore: number | null = null;
  hasBonus = false;
  finalizeSaving = false;

  get maxScore(): number {
    return this.hasBonus ? 150 : 100;
  }

  onBonusChange(): void {
    if (
      !this.hasBonus &&
      this.finalScore !== null &&
      this.finalScore > this.maxScore
    ) {
      this.finalScore = this.maxScore;
    }
  }

  openFinalize(row: LessonResultRowVm): void {
    this.finalizeRow = row;
    this.finalScore = null;
    this.hasBonus = false;
    this.finalizeDialogOpen = true;
  }

  saveFinalize(): void {
    if (!this.finalizeRow || this.finalScore === null) {
      return;
    }

    const request: CompleteLessonRequestDto = {
      studentId: this.finalizeRow.studentId,
      lessonId: this.lessonId,
      finalScore: this.finalScore,
      hasBonus: this.hasBonus,
    };

    this.finalizeSaving = true;
    this.lessonResultsService.complete(request).subscribe({
      next: () => {
        this.finalizeSaving = false;
        this.finalizeDialogOpen = false;
        this.messageService.add({
          severity: "success",
          summary: "בוצע",
          detail: "התוצאה נשמרה בהצלחה",
        });
        this.loadResults();
      },
      error: () => {
        this.finalizeSaving = false;
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "שמירת התוצאה נכשלה",
        });
      },
    });
  }

  ngOnInit(): void {
    const lessonIdParam = this.route.snapshot.paramMap.get("lessonId");
    if (!lessonIdParam) {
      this.navigateToLessons();
      return;
    }

    this.lessonId = Number(lessonIdParam);
    this.loadResults();
  }

  loadResults(): void {
    this.loading = true;
    this.studentsService.getAll().subscribe({
      next: (students: StudentResponseDto[]) =>
        this.loadResultsForStudents(students),
      error: () => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "טעינת רשימת התלמידים נכשלה",
        });
        this.loading = false;
      },
    });
  }

  private loadResultsForStudents(students: StudentResponseDto[]): void {
    if (students.length === 0) {
      this.rows = [];
      this.loading = false;
      return;
    }

    const calls = students.map((student) =>
      this.lessonResultsService.getResult(student.id, this.lessonId).pipe(
        map((result: LessonResultResponseDto) => this.toRow(student, result)),
        catchError(() => of(this.toEmptyRow(student))),
      ),
    );

    forkJoin(calls).subscribe({
      next: (rows) => {
        this.rows = rows;
        this.loading = false;
      },
      error: () => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "טעינת תוצאות השיעור נכשלה",
        });
        this.loading = false;
      },
    });
  }

  private toRow(
    student: StudentResponseDto,
    result: LessonResultResponseDto,
  ): LessonResultRowVm {
    return {
      studentId: student.id,
      studentName: student.fullName ?? "—",
      totalAssignments: result.totalAssignments,
      completedAssignments: result.completedAssignments,
      finalScore: result.finalScore,
      isComplete: result.isComplete,
    };
  }

  private toEmptyRow(student: StudentResponseDto): LessonResultRowVm {
    return {
      studentId: student.id,
      studentName: student.fullName ?? "—",
      totalAssignments: 0,
      completedAssignments: 0,
      finalScore: null,
      isComplete: false,
    };
  }

  navigateToLessons(): void {
    this.router.navigate(["/lessons"]);
  }

  exportExcel(): void {
    this.exporting = true;
    this.lessonResultsService.exportExcel(this.lessonId).subscribe({
      next: (blob) => {
        downloadBlob(blob, `lesson-${this.lessonId}-results.xlsx`);
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
}
