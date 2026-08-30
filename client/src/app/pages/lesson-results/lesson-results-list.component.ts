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
import { TOTAL_POINTS } from "@models/assignment.model";
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
  templateUrl: "./lesson-results-list.component.html",
  styleUrls: ["./lesson-results-list.component.css"],
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
   * ⚠️ מגיע מההצעה שהשרת החזיר, ואינו מחושב כאן. עד כה המסך שלח `hasBonus` והשרת קיבל
   * אותו כלשונו — כלומר הדפדפן קבע לעצמו אם מותר לעבור 100. אחר כך הוא גזר 150 בעצמו,
   * מספר שלא היה התקרה של אף שיעור בפרט; התקרה היא 100 ועוד סכום הבונוסים בשיעור.
   */
  get maxScore(): number {
    return this.suggestion?.maxScore ?? TOTAL_POINTS;
  }

  /** יש בשיעור בונוס כלשהו — כלומר התקרה עוברת 100. */
  get hasBonus(): boolean {
    return this.maxScore > TOTAL_POINTS;
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
