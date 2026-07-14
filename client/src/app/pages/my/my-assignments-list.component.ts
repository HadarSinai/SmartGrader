import { CommonModule } from "@angular/common";
import { Component, OnInit } from "@angular/core";
import { ActivatedRoute, Router, RouterModule } from "@angular/router";
import { forkJoin } from "rxjs";

import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { TableModule } from "primeng/table";
import { TagModule } from "primeng/tag";
import { TooltipModule } from "primeng/tooltip";

import { AssignmentResponseDto } from "@models/assignment.model";
import { LessonResponseDto } from "@models/lesson.model";
import {
    STATUS_LABELS_HE,
    SubmissionResponseDto,
} from "@models/submission.model";
import { AssignmentsService } from "@services/assignments.service";
import { AuthService } from "@services/auth.service";
import { LessonsService } from "@services/lessons.service";
import { SubmissionsService } from "@services/submissions.service";

interface MyAssignmentRow {
  assignment: AssignmentResponseDto;
  submission: SubmissionResponseDto | null;
  statusLabel: string;
  statusSeverity: "success" | "warning" | "danger" | "info" | "secondary";
  statusIcon: string;
}

@Component({
  selector: "app-my-assignments-list",
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
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
              <a class="sg-breadcrumb-link" [routerLink]="['/my', 'lessons']">
                <i class="pi pi-arrow-right" aria-hidden="true"></i>
                חזרה לשיעורים שלי
              </a>
              <div class="sg-title mt-2">
                <div class="sg-h1">
                  התרגילים שלי{{ lesson?.name ? " — " + lesson!.name : "" }}
                </div>
                <div class="sg-h2">הסטטוס האישי שלך בכל תרגיל בשיעור זה</div>
              </div>
            </div>
          </ng-template>

          <div class="sg-table-wrap">
            <p-table
              [value]="rows"
              [loading]="loading"
              dataKey="assignment.id"
              responsiveLayout="stack"
              [breakpoint]="'768px'"
              styleClass="sg-table"
            >
              <ng-template pTemplate="header">
                <tr>
                  <th scope="col">תרגיל</th>
                  <th scope="col">בונוס</th>
                  <th scope="col">סטטוס</th>
                  <th scope="col">ציון</th>
                  <th scope="col"><span class="sr-only">פעולה</span></th>
                </tr>
              </ng-template>

              <ng-template pTemplate="body" let-row>
                <tr>
                  <td>{{ row.assignment.title || "—" }}</td>
                  <td>
                    <span *ngIf="row.assignment.isBonus" class="sg-bonus-chip"
                      >בונוס +{{ row.assignment.bonusValue }}</span
                    >
                    <span *ngIf="!row.assignment.isBonus">—</span>
                  </td>
                  <td>
                    <p-tag
                      [severity]="row.statusSeverity"
                      [icon]="row.statusIcon"
                      [value]="row.statusLabel"
                    ></p-tag>
                  </td>
                  <td>
                    <span
                      *ngIf="row.submission?.score !== null && row.submission"
                      class="sg-score"
                      >{{ row.submission.score }}</span
                    >
                    <span
                      *ngIf="!row.submission || row.submission.score === null"
                      >—</span
                    >
                  </td>
                  <td>
                    <p-button
                      *ngIf="!row.submission"
                      label="הגשת קוד"
                      icon="pi pi-upload"
                      styleClass="sg-btn-primary"
                      (onClick)="submitCode(row.assignment.id)"
                    ></p-button>
                    <p-button
                      *ngIf="row.submission"
                      label="צפייה בפידבק"
                      icon="pi pi-comments"
                      [text]="true"
                      (onClick)="viewFeedback(row.submission.id)"
                    ></p-button>
                  </td>
                </tr>
              </ng-template>

              <ng-template pTemplate="emptymessage">
                <tr>
                  <td colspan="5">
                    <div class="flex flex-column align-items-center gap-2 py-5">
                      <i
                        class="pi pi-inbox text-3xl"
                        style="color: var(--app-muted)"
                        aria-hidden="true"
                      ></i>
                      <span>אין תרגילים בשיעור זה.</span>
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
      .sg-score {
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
export class MyAssignmentsListComponent implements OnInit {
  lessonId!: number;
  lesson: LessonResponseDto | null = null;
  rows: MyAssignmentRow[] = [];
  loading = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private assignmentsService: AssignmentsService,
    private submissionsService: SubmissionsService,
    private lessonsService: LessonsService,
    private auth: AuthService,
  ) {}

  ngOnInit(): void {
    this.lessonId = Number(this.route.snapshot.paramMap.get("lessonId"));
    this.load();
  }

  submitCode(assignmentId: number): void {
    this.router.navigate([
      "/my",
      "lessons",
      this.lessonId,
      "assignments",
      assignmentId,
      "submit",
    ]);
  }

  viewFeedback(submissionId: number): void {
    this.router.navigate(["/my", "submissions", submissionId], {
      queryParams: { lessonId: this.lessonId },
    });
  }

  private load(): void {
    const studentId = this.auth.studentId();
    if (studentId === null) return;

    this.loading = true;
    forkJoin({
      lesson: this.lessonsService.getById(this.lessonId),
      assignments: this.assignmentsService.getByLesson(this.lessonId),
      submissions: this.submissionsService.getByStudent(studentId),
    }).subscribe({
      next: ({ lesson, assignments, submissions }) => {
        this.lesson = lesson;
        this.rows = assignments.map((assignment) =>
          this.toRow(assignment, submissions),
        );
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      },
    });
  }

  private toRow(
    assignment: AssignmentResponseDto,
    submissions: SubmissionResponseDto[],
  ): MyAssignmentRow {
    // Latest submission for this assignment (if any)
    const mine = submissions
      .filter((s) => s.assignmentId === assignment.id)
      .sort(
        (a, b) =>
          new Date(b.submittedAt).getTime() - new Date(a.submittedAt).getTime(),
      );
    const submission = mine[0] ?? null;

    if (!submission) {
      return {
        assignment,
        submission: null,
        statusLabel: "לא הוגש",
        statusSeverity: "secondary",
        statusIcon: "pi pi-minus-circle",
      };
    }

    const status = submission.status;
    const label = (status && STATUS_LABELS_HE[status]) || "ממתין לבדיקה";
    switch (status) {
      case "Done":
        return {
          assignment,
          submission,
          statusLabel: label,
          statusSeverity: "success",
          statusIcon: "pi pi-check-circle",
        };
      case "ProcessingAi":
        return {
          assignment,
          submission,
          statusLabel: label,
          statusSeverity: "info",
          statusIcon: "pi pi-spinner",
        };
      case "CompilationFailed":
      case "AiFailed":
        return {
          assignment,
          submission,
          statusLabel: label,
          statusSeverity: "danger",
          statusIcon: "pi pi-exclamation-triangle",
        };
      default:
        return {
          assignment,
          submission,
          statusLabel: label,
          statusSeverity: "warning",
          statusIcon: "pi pi-clock",
        };
    }
  }
}
