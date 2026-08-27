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
    NO_SUBMISSION_PRESENTATION,
    SubmissionResponseDto,
    statusPresentation,
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
  templateUrl: "./my-assignments-list.component.html",
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

    // ⚠️ מיפוי משותף אחד (STATUS_PRESENTATION) במקום switch מקומי. חמישה עותקים של אותו
    // מיפוי הם חמישה מקומות שיסתרו זה את זה בשינוי הבא — וכבר סתרו.
    const presentation = submission
      ? statusPresentation(submission.status)
      : { ...NO_SUBMISSION_PRESENTATION, label: "לא הוגש" };

    return {
      assignment,
      submission,
      statusLabel: presentation.label,
      statusSeverity: presentation.severity,
      statusIcon: presentation.icon,
    };
  }
}
