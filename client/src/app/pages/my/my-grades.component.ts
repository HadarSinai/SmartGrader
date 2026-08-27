import { CommonModule } from "@angular/common";
import { Component, OnInit } from "@angular/core";
import { Router } from "@angular/router";
import { forkJoin, of } from "rxjs";
import { catchError, map } from "rxjs/operators";

import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { TableModule } from "primeng/table";
import { TagModule } from "primeng/tag";

import { AssignmentResponseDto } from "@models/assignment.model";
import { LessonResultResponseDto } from "@models/lesson-result.model";
import { LessonResponseDto } from "@models/lesson.model";
import {
    SubmissionResponseDto,
    SubmissionStatusSeverity,
    statusPresentation,
} from "@models/submission.model";
import { AssignmentsService } from "@services/assignments.service";
import { AuthService } from "@services/auth.service";
import { LessonResultsService } from "@services/lesson-results.service";
import { LessonsService } from "@services/lessons.service";
import { SubmissionsService } from "@services/submissions.service";

interface FinalScoreRow {
  lesson: LessonResponseDto;
  /** null כאשר השיעור עדיין לא הושלם (404 מ-lesson-results) */
  result: LessonResultResponseDto | null;
}

@Component({
  selector: "app-my-grades",
  standalone: true,
  imports: [CommonModule, TableModule, ButtonModule, CardModule, TagModule],
  template: `
    <section class="sg-page">
      <div class="pt-3 pb-5">
        <p-card styleClass="sg-card">
          <ng-template pTemplate="header">
            <div class="px-4 pt-4 pb-2">
              <div class="sg-title">
                <div class="sg-h1">הציונים שלי</div>
                <div class="sg-h2">כל ההגשות והציונים הסופיים שלך</div>
              </div>
            </div>
          </ng-template>

          <!-- Final scores per lesson (click a row to filter the submissions below) -->
          <div class="px-4 pt-3">
            <h2 class="sg-section-title">ציונים סופיים לשיעורים</h2>
            <p class="sg-section-hint">
              לחצי על שיעור כדי לראות רק את ההגשות שלו בטבלה למטה.
            </p>
          </div>
          <div class="sg-table-wrap">
            <p-table
              [value]="finalScores"
              [loading]="loading"
              dataKey="lesson.id"
              selectionMode="single"
              [(selection)]="selectedLessonRow"
              (onRowSelect)="onLessonSelect()"
              (onRowUnselect)="onLessonUnselect()"
              responsiveLayout="stack"
              [breakpoint]="'768px'"
              styleClass="sg-table sg-table-selectable"
            >
              <ng-template pTemplate="header">
                <tr>
                  <th scope="col">מקצוע</th>
                  <th scope="col">נושא</th>
                  <th scope="col">סטטוס</th>
                  <th scope="col">ציון סופי</th>
                </tr>
              </ng-template>
              <ng-template pTemplate="body" let-row>
                <tr
                  [pSelectableRow]="row"
                  [attr.aria-label]="
                    'הצגת ההגשות של ' + row.lesson.courseName
                  "
                >
                  <td>{{ row.lesson.courseName }}</td>
                  <td>{{ row.lesson.subject || "—" }}</td>
                  <td>
                    <p-tag
                      *ngIf="row.result?.isComplete; else lessonInProgress"
                      severity="success"
                      icon="pi pi-check-circle"
                      value="הושלם"
                    ></p-tag>
                    <ng-template #lessonInProgress>
                      <p-tag
                        severity="info"
                        icon="pi pi-clock"
                        value="בתהליך"
                      ></p-tag>
                    </ng-template>
                  </td>
                  <td>
                    <span
                      *ngIf="
                        row.result?.isComplete &&
                          row.result?.finalScore !== null;
                        else noFinalScore
                      "
                      class="sg-final-score"
                    >
                      {{ row.result?.finalScore }}
                    </span>
                    <ng-template #noFinalScore>—</ng-template>
                  </td>
                </tr>
              </ng-template>
              <ng-template pTemplate="emptymessage">
                <tr>
                  <td colspan="4">
                    <div class="flex flex-column align-items-center gap-2 py-4">
                      <i
                        class="pi pi-inbox text-3xl"
                        style="color: var(--app-muted)"
                        aria-hidden="true"
                      ></i>
                      <span>אין עדיין שיעורים להצגה.</span>
                    </div>
                  </td>
                </tr>
              </ng-template>
            </p-table>
          </div>

          <!-- Submissions — all of them, or only the selected lesson's -->
          <div class="px-4 pt-4">
            <div class="sg-section-head">
              <div>
                <h2 class="sg-section-title">
                  {{
                    selectedLessonRow
                      ? "ההגשות שלי בשיעור " +
                        selectedLessonRow.lesson.courseName
                      : "ההגשות שלי"
                  }}
                </h2>
                <p class="sg-section-hint" *ngIf="selectedLessonRow">
                  מוצגות {{ visibleSubmissions.length }} מתוך
                  {{ submissions.length }} הגשות.
                </p>
              </div>
              <p-button
                *ngIf="selectedLessonRow"
                label="הצגת כל ההגשות"
                icon="pi pi-times"
                [text]="true"
                (onClick)="clearLessonFilter()"
              ></p-button>
            </div>
          </div>
          <div class="sg-table-wrap">
            <p-table
              [value]="visibleSubmissions"
              [loading]="loading"
              [paginator]="visibleSubmissions.length > 10"
              [rows]="10"
              dataKey="id"
              responsiveLayout="stack"
              [breakpoint]="'768px'"
              styleClass="sg-table"
            >
              <ng-template pTemplate="header">
                <tr>
                  <th scope="col">תרגיל</th>
                  <th scope="col" *ngIf="!selectedLessonRow">שיעור</th>
                  <th scope="col">תאריך הגשה</th>
                  <th scope="col">סטטוס</th>
                  <th scope="col">ציון</th>
                  <th scope="col"><span class="sr-only">פעולות</span></th>
                </tr>
              </ng-template>
              <ng-template pTemplate="body" let-submission>
                <tr>
                  <td>{{ submission.assignmentName || "—" }}</td>
                  <td *ngIf="!selectedLessonRow">
                    {{ lessonNameFor(submission.assignmentId) }}
                  </td>
                  <td>
                    {{ submission.submittedAt | date: "dd.MM.yy HH:mm" }}
                  </td>
                  <td>
                    <p-tag
                      [severity]="statusSeverity(submission.status)"
                      [icon]="statusIcon(submission.status)"
                      [value]="statusLabel(submission.status)"
                    ></p-tag>
                  </td>
                  <td>
                    {{ submission.score !== null ? submission.score : "—" }}
                  </td>
                  <td>
                    <p-button
                      label="צפייה בפידבק"
                      icon="pi pi-comments"
                      [text]="true"
                      (onClick)="viewFeedback(submission.id)"
                    ></p-button>
                  </td>
                </tr>
              </ng-template>
              <ng-template pTemplate="emptymessage">
                <tr>
                  <td [attr.colspan]="selectedLessonRow ? 5 : 6">
                    <div class="flex flex-column align-items-center gap-2 py-4">
                      <i
                        class="pi pi-inbox text-3xl"
                        style="color: var(--app-muted)"
                        aria-hidden="true"
                      ></i>
                      <span>{{
                        selectedLessonRow
                          ? "אין הגשות בשיעור הזה."
                          : "אין עדיין הגשות."
                      }}</span>
                    </div>
                  </td>
                </tr>
              </ng-template>
            </p-table>
          </div>
        </p-card>
      </div>
    </section>
  `,
  styles: [
    `
      .sg-section-title {
        font-size: var(--text-lg);
        font-weight: 700;
        color: var(--app-text-strong);
        margin: 0;
      }

      .sg-section-hint {
        font-size: var(--text-sm);
        color: var(--app-muted);
        margin: 0.25rem 0 0;
      }

      .sg-section-head {
        display: flex;
        align-items: flex-start;
        justify-content: space-between;
        gap: var(--space-3);
        flex-wrap: wrap;
      }

      /* Selectable lesson rows — affordance + selected state */
      :host ::ng-deep .sg-table-selectable tbody tr {
        cursor: pointer;
      }

      :host ::ng-deep
        .sg-table-selectable
        tbody
        tr.p-highlight
        > td:first-child {
        box-shadow: inset -3px 0 0 0 var(--status-success-ink);
      }
    `,
  ],
})
export class MyGradesComponent implements OnInit {
  submissions: SubmissionResponseDto[] = [];
  /** ההגשות המוצגות בפועל — כל ההגשות, או רק אלה של השיעור הנבחר */
  visibleSubmissions: SubmissionResponseDto[] = [];
  finalScores: FinalScoreRow[] = [];
  selectedLessonRow: FinalScoreRow | null = null;
  loading = false;

  /** assignmentId → lessonId, נבנה מהתרגילים של כל השיעורים (ל-SubmissionResponseDto אין lessonId) */
  private assignmentToLesson = new Map<number, number>();
  private lessonNames = new Map<number, string>();

  constructor(
    private submissionsService: SubmissionsService,
    private lessonsService: LessonsService,
    private lessonResultsService: LessonResultsService,
    private assignmentsService: AssignmentsService,
    private auth: AuthService,
    private router: Router,
  ) {}

  ngOnInit(): void {
    this.load();
  }

  viewFeedback(submissionId: number): void {
    this.router.navigate(["/my", "submissions", submissionId]);
  }

  onLessonSelect(): void {
    this.applyLessonFilter();
  }

  onLessonUnselect(): void {
    this.selectedLessonRow = null;
    this.applyLessonFilter();
  }

  clearLessonFilter(): void {
    this.selectedLessonRow = null;
    this.applyLessonFilter();
  }

  lessonNameFor(assignmentId: number): string {
    const lessonId = this.assignmentToLesson.get(assignmentId);
    if (lessonId === undefined) return "—";
    return this.lessonNames.get(lessonId) || "—";
  }

  /** מסנן את ההגשות לפי השיעור הנבחר, דרך מפת assignmentId → lessonId */
  private applyLessonFilter(): void {
    const selectedLessonId = this.selectedLessonRow?.lesson.id ?? null;
    if (selectedLessonId === null) {
      this.visibleSubmissions = this.submissions;
      return;
    }
    this.visibleSubmissions = this.submissions.filter(
      (submission) =>
        this.assignmentToLesson.get(submission.assignmentId) ===
        selectedLessonId,
    );
  }

  // ⚠️ מיפוי משותף אחד (STATUS_PRESENTATION). כאן ישב עותק שלישי, והוא נבדל מהאחרים:
  // CompilationFailed קיבל pi-times-circle בעוד ששאר המסכים נתנו לו pi-exclamation-triangle.
  statusLabel(status: string | null): string {
    return statusPresentation(status).label;
  }

  statusSeverity(status: string | null): SubmissionStatusSeverity {
    return statusPresentation(status).severity;
  }

  statusIcon(status: string | null): string {
    return statusPresentation(status).icon;
  }

  private load(): void {
    const studentId = this.auth.studentId();
    if (studentId === null) return;

    this.loading = true;
    forkJoin({
      submissions: this.submissionsService.getByStudent(studentId),
      lessons: this.lessonsService.getAll(),
    }).subscribe({
      next: ({ submissions, lessons }) => {
        this.submissions = [...submissions].sort(
          (a, b) =>
            new Date(b.submittedAt).getTime() -
            new Date(a.submittedAt).getTime(),
        );
        this.visibleSubmissions = this.submissions;
        this.lessonNames = new Map(
          lessons.map((lesson) => [lesson.id, lesson.courseName]),
        );
        this.loadAssignmentMap(lessons);
        this.loadFinalScores(studentId, lessons);
      },
      error: () => {
        this.loading = false;
      },
    });
  }

  /**
   * בונה assignmentId → lessonId עבור כל השיעורים, כדי לאפשר סינון הגשות לפי שיעור.
   * SubmissionResponseDto לא מחזיק lessonId, ואין endpoint לתרגיל בודד.
   */
  private loadAssignmentMap(lessons: LessonResponseDto[]): void {
    if (lessons.length === 0) {
      this.assignmentToLesson = new Map();
      return;
    }
    forkJoin(
      lessons.map((lesson) =>
        this.assignmentsService.getByLesson(lesson.id).pipe(
          map((assignments: AssignmentResponseDto[]) => ({
            lessonId: lesson.id,
            assignments,
          })),
          catchError(() =>
            of({ lessonId: lesson.id, assignments: [] as AssignmentResponseDto[] }),
          ),
        ),
      ),
    ).subscribe({
      next: (groups) => {
        const map = new Map<number, number>();
        for (const group of groups) {
          for (const assignment of group.assignments) {
            map.set(assignment.id, group.lessonId);
          }
        }
        this.assignmentToLesson = map;
        // ההגשות כבר הוצגו; מרעננים אם יש סינון פעיל
        this.applyLessonFilter();
      },
    });
  }

  private loadFinalScores(
    studentId: number,
    lessons: LessonResponseDto[],
  ): void {
    if (lessons.length === 0) {
      this.finalScores = [];
      this.loading = false;
      return;
    }
    forkJoin(
      lessons.map((lesson) =>
        this.lessonResultsService.getResult(studentId, lesson.id).pipe(
          map(
            (result: LessonResultResponseDto): FinalScoreRow => ({
              lesson,
              result,
            }),
          ),
          // 404 → אין עדיין תוצאה לשיעור; מוצג כ"בתהליך"
          catchError(() => of<FinalScoreRow>({ lesson, result: null })),
        ),
      ),
    ).subscribe({
      next: (rows: FinalScoreRow[]) => {
        this.finalScores = rows;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      },
    });
  }
}
