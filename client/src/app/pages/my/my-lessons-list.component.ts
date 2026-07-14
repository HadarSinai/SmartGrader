import { CommonModule } from "@angular/common";
import { Component, OnInit } from "@angular/core";
import { Router } from "@angular/router";
import { forkJoin, of } from "rxjs";
import { catchError, map } from "rxjs/operators";

import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { TableModule } from "primeng/table";
import { TagModule } from "primeng/tag";
import { TooltipModule } from "primeng/tooltip";

import { LessonResultResponseDto } from "@models/lesson-result.model";
import { LessonResponseDto } from "@models/lesson.model";
import { AuthService } from "@services/auth.service";
import { LessonResultsService } from "@services/lesson-results.service";
import { LessonsService } from "@services/lessons.service";

interface MyLessonRow {
  lesson: LessonResponseDto;
  result: LessonResultResponseDto | null;
}

@Component({
  selector: "app-my-lessons-list",
  standalone: true,
  imports: [
    CommonModule,
    TableModule,
    ButtonModule,
    CardModule,
    TagModule,
    TooltipModule,
  ],
  template: `
    <section class="sg-page">
      <div class="pt-3 pb-5">
        <p-card styleClass="sg-card">
          <ng-template pTemplate="header">
            <div class="px-4 pt-4 pb-2">
              <div class="sg-title">
                <div class="sg-h1">השיעורים שלי</div>
                <div class="sg-h2">כל השיעורים והסטטוס האישי שלך בכל אחד</div>
              </div>
            </div>
          </ng-template>

          <div class="sg-table-wrap">
            <p-table
              [value]="rows"
              [loading]="loading"
              [paginator]="rows.length > 10"
              [rows]="10"
              dataKey="lesson.id"
              responsiveLayout="stack"
              [breakpoint]="'768px'"
              styleClass="sg-table"
            >
              <ng-template pTemplate="header">
                <tr>
                  <th scope="col">שם השיעור</th>
                  <th scope="col">נושא</th>
                  <th scope="col">תאריך</th>
                  <th scope="col">סטטוס</th>
                  <th scope="col">ציון סופי</th>
                  <th scope="col"><span class="sr-only">פעולות</span></th>
                </tr>
              </ng-template>

              <ng-template pTemplate="body" let-row>
                <tr>
                  <td>{{ row.lesson.name || "—" }}</td>
                  <td>{{ row.lesson.subject || "—" }}</td>
                  <td>{{ row.lesson.lessonDate | date: "dd.MM.yy HH:mm" }}</td>
                  <td>
                    <p-tag
                      *ngIf="row.result?.isComplete; else inProgress"
                      severity="success"
                      icon="pi pi-check-circle"
                      value="הושלם"
                    ></p-tag>
                    <ng-template #inProgress>
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
                        else noScore
                      "
                      class="sg-final-score"
                    >
                      {{ row.result?.finalScore }}
                    </span>
                    <ng-template #noScore>—</ng-template>
                  </td>
                  <td>
                    <p-button
                      label="לתרגילים"
                      icon="pi pi-arrow-left"
                      iconPos="left"
                      [text]="true"
                      (onClick)="openLesson(row.lesson.id)"
                      [ariaLabel]="
                        'מעבר לתרגילים של ' + (row.lesson.name || 'השיעור')
                      "
                    ></p-button>
                  </td>
                </tr>
              </ng-template>

              <ng-template pTemplate="emptymessage">
                <tr>
                  <td colspan="6">
                    <div class="flex flex-column align-items-center gap-2 py-5">
                      <i
                        class="pi pi-inbox text-3xl"
                        style="color: var(--app-muted)"
                        aria-hidden="true"
                      ></i>
                      <span>אין שיעורים להצגה.</span>
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
export class MyLessonsListComponent implements OnInit {
  rows: MyLessonRow[] = [];
  loading = false;

  constructor(
    private lessonsService: LessonsService,
    private lessonResultsService: LessonResultsService,
    private auth: AuthService,
    private router: Router,
  ) {}

  ngOnInit(): void {
    this.loadLessons();
  }

  openLesson(lessonId: number): void {
    this.router.navigate(["/my", "lessons", lessonId, "assignments"]);
  }

  private loadLessons(): void {
    const studentId = this.auth.studentId();
    if (studentId === null) return;

    this.loading = true;
    this.lessonsService.getAll().subscribe({
      next: (lessons: LessonResponseDto[]) => {
        if (lessons.length === 0) {
          this.rows = [];
          this.loading = false;
          return;
        }
        forkJoin(
          lessons.map((lesson) =>
            this.lessonResultsService.getResult(studentId, lesson.id).pipe(
              map(
                (result: LessonResultResponseDto): MyLessonRow => ({
                  lesson,
                  result,
                }),
              ),
              // 404 (no result yet) → "בתהליך"
              catchError(() => of<MyLessonRow>({ lesson, result: null })),
            ),
          ),
        ).subscribe({
          next: (rows: MyLessonRow[]) => {
            this.rows = rows;
            this.loading = false;
          },
          error: () => {
            this.loading = false;
          },
        });
      },
      error: () => {
        this.loading = false;
      },
    });
  }
}
