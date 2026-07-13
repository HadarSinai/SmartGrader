import { CommonModule } from "@angular/common";
import { Component, OnInit, inject } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { forkJoin, of } from "rxjs";
import { catchError, map } from "rxjs/operators";

import { MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { DataViewModule } from "primeng/dataview";
import { TableModule } from "primeng/table";
import { TagModule } from "primeng/tag";
import { TooltipModule } from "primeng/tooltip";

import { LessonResultResponseDto } from "@models/lesson-result.model";
import { StudentResponseDto } from "@models/student.model";
import { LessonResultsService } from "@services/lesson-results.service";
import { StudentsService } from "@services/students.service";

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
    ButtonModule,
    CardModule,
    TableModule,
    DataViewModule,
    TagModule,
    TooltipModule,
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
                <div class="sg-h1">תוצאות שיעור</div>
                <div class="sg-h2">מעקב אחר התקדמות התלמידים בשיעור</div>
              </div>

              <p-button
                label="חזרה לשיעורים"
                icon="pi pi-arrow-left"
                severity="secondary"
                [outlined]="true"
                (onClick)="navigateToLessons()"
              ></p-button>
            </div>
          </ng-template>

          <!-- Desktop table -->
          <div class="sg-table-wrap desktop-only">
            <p-table
              [value]="rows"
              [loading]="loading"
              responsiveLayout="scroll"
              styleClass="sg-table"
            >
              <ng-template pTemplate="header">
                <tr>
                  <th class="text-center">שם התלמיד/ה</th>
                  <th class="text-center">התקדמות</th>
                  <th class="text-center">ציון סופי</th>
                  <th class="text-center">סטטוס</th>
                  <th class="text-center">פעולות</th>
                </tr>
              </ng-template>

              <ng-template pTemplate="body" let-row>
                <tr>
                  <td class="text-center">{{ row.studentName }}</td>
                  <td class="text-center">
                    <div class="sg-frac" aria-label="התקדמות בתרגילים">
                      <div class="sg-frac-top">
                        {{ row.completedAssignments }}
                      </div>
                      <div class="sg-frac-line"></div>
                      <div class="sg-frac-bot">
                        {{ row.totalAssignments }}
                      </div>
                    </div>
                  </td>
                  <td class="text-center">
                    {{ row.finalScore ?? "טרם נקבע" }}
                  </td>
                  <td class="text-center">
                    <p-tag
                      *ngIf="row.isComplete"
                      severity="success"
                      value="הושלם"
                    ></p-tag>
                    <p-tag
                      *ngIf="!row.isComplete"
                      severity="info"
                      value="בתהליך"
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
                      [disabled]="true"
                      pTooltip="פונקציונליות זו תופעל בקרוב"
                      tooltipPosition="top"
                      aria-describedby="finalizeHint"
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
                    אין תלמידים להצגה.
                  </td>
                </tr>
              </ng-template>
            </p-table>
          </div>

          <!-- Mobile cards -->
          <div class="mobile-only px-3 pb-3">
            <p-dataView [value]="rows" [loading]="loading" layout="list">
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
                      ></p-tag>
                      <p-tag
                        *ngIf="!item.isComplete"
                        severity="info"
                        value="בתהליך"
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
                        [disabled]="true"
                        pTooltip="פונקציונליות זו תופעל בקרוב"
                        tooltipPosition="top"
                        aria-describedby="finalizeHint"
                      ></p-button>
                    </div>
                  </div>
                </div>
              </ng-template>
            </p-dataView>
          </div>
        </p-card>
      </div>
      <span id="finalizeHint" class="sr-only">פונקציונליות זו תופעל בקרוב</span>
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
}
