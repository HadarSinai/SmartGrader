import { CommonModule } from "@angular/common";
import { Component, OnInit } from "@angular/core";
import { Router } from "@angular/router";
import { forkJoin, of } from "rxjs";
import { catchError, map } from "rxjs/operators";

import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { TableModule } from "primeng/table";
import { TagModule } from "primeng/tag";

import { LessonResultResponseDto } from "@models/lesson-result.model";
import { LessonResponseDto } from "@models/lesson.model";
import {
    STATUS_LABELS_HE,
    SubmissionResponseDto,
} from "@models/submission.model";
import { AuthService } from "@services/auth.service";
import { LessonResultsService } from "@services/lesson-results.service";
import { LessonsService } from "@services/lessons.service";
import { SubmissionsService } from "@services/submissions.service";

interface FinalScoreRow {
  lesson: LessonResponseDto;
  result: LessonResultResponseDto;
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

          <!-- Final scores per completed lesson -->
          <div class="px-4 pt-3">
            <h2 class="sg-section-title">ציונים סופיים לשיעורים</h2>
          </div>
          <div class="sg-table-wrap">
            <p-table
              [value]="finalScores"
              [loading]="loading"
              dataKey="lesson.id"
              responsiveLayout="stack"
              [breakpoint]="'768px'"
              styleClass="sg-table"
            >
              <ng-template pTemplate="header">
                <tr>
                  <th scope="col">שיעור</th>
                  <th scope="col">נושא</th>
                  <th scope="col">ציון סופי</th>
                </tr>
              </ng-template>
              <ng-template pTemplate="body" let-row>
                <tr>
                  <td>{{ row.lesson.name || "—" }}</td>
                  <td>{{ row.lesson.subject || "—" }}</td>
                  <td>
                    <span class="sg-final-score">{{
                      row.result.finalScore !== null
                        ? row.result.finalScore
                        : "—"
                    }}</span>
                  </td>
                </tr>
              </ng-template>
              <ng-template pTemplate="emptymessage">
                <tr>
                  <td colspan="3">
                    <div class="flex flex-column align-items-center gap-2 py-4">
                      <i
                        class="pi pi-inbox text-3xl"
                        style="color: var(--app-muted)"
                        aria-hidden="true"
                      ></i>
                      <span>אין עדיין ציונים סופיים.</span>
                    </div>
                  </td>
                </tr>
              </ng-template>
            </p-table>
          </div>

          <!-- All submissions -->
          <div class="px-4 pt-4">
            <h2 class="sg-section-title">ההגשות שלי</h2>
          </div>
          <div class="sg-table-wrap">
            <p-table
              [value]="submissions"
              [loading]="loading"
              [paginator]="submissions.length > 10"
              [rows]="10"
              dataKey="id"
              responsiveLayout="stack"
              [breakpoint]="'768px'"
              styleClass="sg-table"
            >
              <ng-template pTemplate="header">
                <tr>
                  <th scope="col">תרגיל</th>
                  <th scope="col">תאריך הגשה</th>
                  <th scope="col">סטטוס</th>
                  <th scope="col">ציון</th>
                  <th scope="col"><span class="sr-only">פעולות</span></th>
                </tr>
              </ng-template>
              <ng-template pTemplate="body" let-submission>
                <tr>
                  <td>{{ submission.assignmentName || "—" }}</td>
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
                  <td colspan="5">
                    <div class="flex flex-column align-items-center gap-2 py-4">
                      <i
                        class="pi pi-inbox text-3xl"
                        style="color: var(--app-muted)"
                        aria-hidden="true"
                      ></i>
                      <span>אין עדיין הגשות.</span>
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

      .sg-final-score {
        font-weight: 800;
        color: var(--status-success-ink);
      }

      .sr-only {
        position: absolute;
        width: 1px;
        height: 1px;
        overflow: hidden;
        clip: rect(0 0 0 0);
        white-space: nowrap;
      }
    `,
  ],
})
export class MyGradesComponent implements OnInit {
  submissions: SubmissionResponseDto[] = [];
  finalScores: FinalScoreRow[] = [];
  loading = false;

  constructor(
    private submissionsService: SubmissionsService,
    private lessonsService: LessonsService,
    private lessonResultsService: LessonResultsService,
    private auth: AuthService,
    private router: Router,
  ) {}

  ngOnInit(): void {
    this.load();
  }

  viewFeedback(submissionId: number): void {
    this.router.navigate(["/my", "submissions", submissionId]);
  }

  statusLabel(status: string | null): string {
    return (status && STATUS_LABELS_HE[status]) || "ממתין לבדיקה";
  }

  statusSeverity(
    status: string | null,
  ): "success" | "warning" | "danger" | "info" {
    switch (status) {
      case "Done":
        return "success";
      case "ProcessingAi":
        return "info";
      case "CompilationFailed":
      case "AiFailed":
        return "danger";
      default:
        return "warning";
    }
  }

  statusIcon(status: string | null): string {
    switch (status) {
      case "Done":
        return "pi pi-check-circle";
      case "ProcessingAi":
        return "pi pi-spinner";
      case "CompilationFailed":
        return "pi pi-times-circle";
      case "AiFailed":
        return "pi pi-exclamation-triangle";
      default:
        return "pi pi-clock";
    }
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
        this.loadFinalScores(studentId, lessons);
      },
      error: () => {
        this.loading = false;
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
          map((result: LessonResultResponseDto) => ({ lesson, result })),
          // 404 → no result for this lesson; skip it
          catchError(() => of(null)),
        ),
      ),
    ).subscribe({
      next: (rows) => {
        this.finalScores = rows.filter(
          (row): row is FinalScoreRow => row !== null && row.result.isComplete,
        );
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      },
    });
  }
}
