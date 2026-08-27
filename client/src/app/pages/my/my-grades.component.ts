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
  templateUrl: "./my-grades.component.html",
  styleUrls: ["./my-grades.component.css"],
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
