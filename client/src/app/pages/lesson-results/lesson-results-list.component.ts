import { CommonModule } from "@angular/common";
import { Component, OnInit, inject } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { forkJoin, of } from "rxjs";
import { catchError, map } from "rxjs/operators";

import { ConfirmationService, MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { ConfirmDialogModule } from "primeng/confirmdialog";
import { DataViewModule } from "primeng/dataview";
import { DialogModule } from "primeng/dialog";
import { DropdownModule } from "primeng/dropdown";
import { InputNumberModule } from "primeng/inputnumber";
import { InputTextModule } from "primeng/inputtext";
import { InputTextareaModule } from "primeng/inputtextarea";
import { TableModule } from "primeng/table";
import { TagModule } from "primeng/tag";
import { TooltipModule } from "primeng/tooltip";

import { FormsModule } from "@angular/forms";
import {
  CompleteLessonRequestDto,
  LessonResultResponseDto,
  LessonScoreSuggestionDto,
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
  /** הציון נקבע ידנית ולא התקבל מהחישוב — ר' LessonResult.CompleteWithOverride בשרת. */
  isFinalScoreOverridden: boolean;
  /** מה חושב ומה הסיבה לשינוי. null כשהציון מחושב. */
  overrideTooltip: string | null;
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
    ConfirmDialogModule,
    InputNumberModule,
    InputTextareaModule,
    DropdownModule,
    InputTextModule,
  ],
  providers: [ConfirmationService],
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
                    <!-- בלי הסימון הזה אי אפשר להבחין בין ציון שהמערכת חישבה
                         לציון שהוקלד, וזה בדיוק מה שהתיעוד נועד לשמר -->
                    <i
                      *ngIf="row.isFinalScoreOverridden"
                      class="pi pi-user-edit sg-override-mark"
                      [pTooltip]="row.overrideTooltip!"
                      tooltipPosition="top"
                      [attr.aria-label]="row.overrideTooltip"
                    ></i>
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
                    <!-- ⚠️ עד עכשיו ציון סופי שגוי לא היה ניתן לתיקון בשום דרך.
                         הפתיחה גם משחררת את ההגשות של אותה תלמידה בשיעור. -->
                    <p-button
                      *ngIf="row.isComplete"
                      label="פתיחה מחדש"
                      icon="pi pi-lock-open"
                      [text]="true"
                      [attr.aria-label]="
                        'פתיחת השיעור מחדש עבור ' + row.studentName
                      "
                      (onClick)="confirmReopen(row)"
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
                        <i
                          *ngIf="item.isFinalScoreOverridden"
                          class="pi pi-user-edit sg-override-mark"
                          [pTooltip]="item.overrideTooltip!"
                          tooltipPosition="top"
                          [attr.aria-label]="item.overrideTooltip"
                        ></i>
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
                      <p-button
                        *ngIf="item.isComplete"
                        label="פתיחה מחדש"
                        icon="pi pi-lock-open"
                        [outlined]="true"
                        [attr.aria-label]="
                          'פתיחת השיעור מחדש עבור ' + item.studentName
                        "
                        (onClick)="confirmReopen(item)"
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
        [style]="{ width: '30rem' }"
        [draggable]="false"
        [resizable]="false"
      >
        <div class="flex flex-column gap-3" *ngIf="finalizeRow">
          <div>
            קביעת ציון סופי עבור
            <strong>{{ finalizeRow.studentName }}</strong>
          </div>

          <div
            *ngIf="suggestionLoading"
            class="flex align-items-center gap-2 text-color-secondary"
          >
            <i class="pi pi-spin pi-spinner" aria-hidden="true"></i>
            טוענת את ציוני התרגילים...
          </div>

          <!-- 🔴 המספרים האלה כבר היו במערכת ומעולם לא הוצגו כאן: המורה חישבה ממוצע
               ביד לכל תלמידה. הציון נשאר הצעה הניתנת לעריכה — היא עדיין מחליטה. -->
          <div *ngIf="!suggestionLoading && suggestion" class="sg-suggestion">
            <div
              class="sg-suggestion__row"
              *ngFor="let item of suggestion.assignmentScores"
            >
              <span class="sg-suggestion__name">
                {{ item.title || "תרגיל" }}
                <span class="sg-bonus-chip" *ngIf="item.isBonus">בונוס</span>
              </span>
              <span class="sg-suggestion__score" *ngIf="item.score !== null">
                {{ item.score }}
              </span>
              <span class="sg-suggestion__missing" *ngIf="item.score === null">
                {{ item.status }}
              </span>
            </div>

            <div class="sg-suggestion__total">
              <span>ממוצע</span>
              <span>{{
                suggestion.suggestedScore !== null
                  ? suggestion.suggestedScore
                  : "—"
              }}</span>
            </div>

            <!-- ⚠️ ממוצע שמדלג על תרגיל בשקט נראה נכון ואינו נכון -->
            <small class="sg-hint" *ngIf="suggestion.ungradedCount > 0">
              {{ suggestion.ungradedCount }} תרגילים ללא ציון לא נכללו בממוצע,
              שחושב על {{ suggestion.gradedCount }} תרגילים.
            </small>
            <small class="sg-hint" *ngIf="suggestion.gradedCount === 0">
              אף תרגיל לא נבדק, ולכן אין ממוצע להציע — אפשר להזין ציון ידנית.
            </small>
          </div>

          <!-- ⚠️ תיבת סימון "כולל בונוס" הוסרה: היא נשלחה לשרת וקבעה את תקרת הציון,
               כלומר המסך קבע לעצמו את הטווח החוקי. הבונוס נגזר מהתרגילים בפועל. -->
          <div class="sg-hint" *ngIf="!suggestionLoading && suggestion?.hasBonus">
            <i class="pi pi-star" aria-hidden="true"></i>
            בשיעור יש תרגיל בונוס, ולכן הציון הסופי יכול להגיע עד {{ maxScore }}.
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
            <small class="sg-hint block mt-1">
              הערך ממולא מראש מהציון שהמערכת חישבה. שינוי שלו הוא דריסה — היא
              נרשמת עם שמך ועם הסיבה.
            </small>
          </div>

          <!-- הציון שהוזן שונה מהמחושב: סיבה חובה, בדיוק כמו בדריסת ציון של הגשה בודדת -->
          <div *ngIf="isOverride">
            <label class="sg-label" for="overrideReason"
              >סיבה לשינוי הציון *</label
            >
            <textarea
              id="overrideReason"
              pInputTextarea
              [(ngModel)]="overrideReason"
              [rows]="2"
              class="w-full"
              placeholder="למה הציון הסופי שונה מהמחושב?"
            ></textarea>
            <small class="sg-hint block mt-1">
              המערכת חישבה
              {{
                suggestion?.suggestedScore !== null &&
                suggestion?.suggestedScore !== undefined
                  ? suggestion?.suggestedScore
                  : "אין ציון מחושב — אף תרגיל לא נבדק"
              }}. הציון שהוזן נשמר לצד המחושב, כדי שיהיה אפשר לדעת בדיעבד מה
              השתנה.
            </small>
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
            [disabled]="finalScore === null || (isOverride && !overrideReason.trim())"
            (onClick)="saveFinalize()"
          ></p-button>
        </ng-template>
      </p-dialog>

      <p-confirmDialog></p-confirmDialog>
    </section>
  `,
  styles: [
    `
      .sg-override-mark {
        margin-inline-start: var(--space-1);
        color: var(--app-text-muted, var(--text-color-secondary));
        font-size: 0.85em;
      }

      .sg-suggestion {
        display: flex;
        flex-direction: column;
        gap: var(--space-1);
        padding: var(--space-3);
        border: 1px solid var(--app-border);
        border-radius: var(--radius-md);
        background: var(--app-surface-2);
      }

      .sg-suggestion__row {
        display: flex;
        align-items: baseline;
        justify-content: space-between;
        gap: var(--space-2);
      }

      .sg-suggestion__name {
        display: flex;
        align-items: center;
        gap: var(--space-2);
      }

      .sg-suggestion__score {
        font-weight: 700;
      }

      /* תרגיל שאינו נכנס לממוצע — מוצג מעומעם ובטקסט, לא כמספר */
      .sg-suggestion__missing {
        font-size: var(--text-sm);
        color: var(--app-muted);
      }

      .sg-suggestion__total {
        display: flex;
        align-items: baseline;
        justify-content: space-between;
        gap: var(--space-2);
        margin-top: var(--space-2);
        padding-top: var(--space-2);
        border-top: 1px solid var(--app-border);
        font-weight: 800;
      }
    `,
  ],
})
export class LessonResultsListComponent implements OnInit {
  private readonly lessonResultsService = inject(LessonResultsService);
  private readonly studentsService = inject(StudentsService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

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
  overrideReason = "";
  finalizeSaving = false;
  suggestion: LessonScoreSuggestionDto | null = null;
  suggestionLoading = false;

  /**
   * ⚠️ נגזר מההצעה שהשרת החזיר, לא מתיבת סימון. עד כה המסך שלח `hasBonus` והשרת קיבל
   * אותו כלשונו — כלומר הדפדפן קבע לעצמו אם מותר לעבור 100.
   */
  get maxScore(): number {
    return this.suggestion?.hasBonus ? 150 : 100;
  }

  /**
   * הציון שהוזן שונה מזה שהמערכת חישבה — ולכן דורש סיבה.
   * הסף זהה ל-`LessonScoreCalculator.Matches` בשרת: המחושב מעוגל לספרה אחת, והשוואת
   * שוויון מדויקת הייתה מסמנת את ההצעה עצמה כדריסה.
   */
  get isOverride(): boolean {
    if (this.finalScore === null || !this.suggestion) {
      return false;
    }
    if (this.suggestion.suggestedScore === null) {
      // אין ציון מחושב כלל — כל ציון שיוזן הוא הכרעה של המורה.
      return true;
    }
    return Math.abs(this.suggestion.suggestedScore - this.finalScore) >= 0.05;
  }

  /**
   * ⚠️ הפתיחה טוענת את הציונים שכבר חושבו. עד כה הדיאלוג נפתח עם null והמורה חישבה
   * ממוצע ביד, לכל תלמידה, בזמן שכל המספרים כבר היו במערכת.
   */
  openFinalize(row: LessonResultRowVm): void {
    this.finalizeRow = row;
    this.finalScore = null;
    this.overrideReason = "";
    this.suggestion = null;
    this.suggestionLoading = true;
    this.finalizeDialogOpen = true;

    this.lessonResultsService
      .getScoreSuggestion(row.studentId, this.lessonId)
      .subscribe({
        next: (suggestion: LessonScoreSuggestionDto) => {
          this.suggestion = suggestion;
          this.suggestionLoading = false;
          this.finalScore = suggestion.suggestedScore;
        },
        error: () => {
          // ההצעה היא נוחות, לא תנאי: כשלון בטעינתה משאיר הזנה ידנית מלאה, בדיוק
          // כמו שהיה עד היום. ההודעה מגיעה מה-interceptor.
          this.suggestionLoading = false;
        },
      });
  }

  confirmReopen(row: LessonResultRowVm): void {
    this.confirmationService.confirm({
      message: `לפתוח מחדש את הציון הסופי של ${row.studentName}? הציון הנוכחי (${row.finalScore ?? "—"}) יימחק, וההגשות של התלמידה בשיעור הזה ייפתחו שוב.`,
      header: "פתיחת שיעור מחדש",
      icon: "pi pi-exclamation-triangle",
      acceptLabel: "פתיחה מחדש",
      rejectLabel: "ביטול",
      accept: () => this.reopen(row),
    });
  }

  private reopen(row: LessonResultRowVm): void {
    this.lessonResultsService.reopen(row.studentId, this.lessonId).subscribe({
      next: () => {
        this.messageService.add({
          severity: "success",
          summary: "בוצע",
          detail: "השיעור נפתח מחדש",
        });
        this.loadResults();
      },
      error: () => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "פתיחת השיעור מחדש נכשלה",
        });
      },
    });
  }

  saveFinalize(): void {
    if (!this.finalizeRow || this.finalScore === null) {
      return;
    }

    // ⚠️ finalScore הוא בקשת דריסה, לא הציון: השרת גוזר את הציון מההגשות ומתעלם מהערך
    // הזה כשהוא זהה למחושב. הסיבה נשלחת רק כשיש חריגה — היא יומן הביקורת.
    const request: CompleteLessonRequestDto = {
      studentId: this.finalizeRow.studentId,
      lessonId: this.lessonId,
      finalScore: this.finalScore,
      overrideReason: this.isOverride ? this.overrideReason.trim() : null,
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
      isFinalScoreOverridden: result.isFinalScoreOverridden,
      overrideTooltip: result.isFinalScoreOverridden
        ? `ציון שנקבע ידנית. המערכת חישבה ${result.computedScore ?? "—"}. סיבה: ${
            result.finalScoreOverrideReason ?? "—"
          }`
        : null,
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
      isFinalScoreOverridden: false,
      overrideTooltip: null,
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
