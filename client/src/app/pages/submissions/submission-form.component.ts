import { CommonModule } from "@angular/common";
import { Component, OnInit, inject } from "@angular/core";
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";
import { AssignmentResponseDto } from "@models/assignment.model";
import { LessonResponseDto } from "@models/lesson.model";
import {
  CreateSubmissionRequestDto,
  SubmissionResponseDto,
  UpdateSubmissionRequestDto,
} from "@models/submission.model";
import { AssignmentsService } from "@services/assignments.service";
import { LessonsService } from "@services/lessons.service";
import { SubmissionsService } from "@services/submissions.service";
import { MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { DropdownModule } from "primeng/dropdown";
import { InputTextareaModule } from "primeng/inputtextarea";

interface AssignmentOption {
  label: string;
  value: number;
}

@Component({
  selector: "app-submission-form",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CardModule,
    DropdownModule,
    ButtonModule,
    InputTextareaModule,
  ],
  templateUrl: "./submission-form.component.html",
  styleUrls: ["./submission-form.component.css"],
})
export class SubmissionFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly submissionsService = inject(SubmissionsService);
  private readonly assignmentsService = inject(AssignmentsService);
  private readonly lessonsService = inject(LessonsService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly messageService = inject(MessageService);

  form: FormGroup;
  loading = false;
  isEditMode = false;
  studentId!: number;
  submissionId: number | null = null;
  submission: SubmissionResponseDto | null = null;

  lessons: LessonResponseDto[] = [];
  lessonOptions: AssignmentOption[] = [];
  assignments: AssignmentResponseDto[] = [];
  assignmentOptions: AssignmentOption[] = [];

  constructor() {
    this.form = this.fb.group({
      lessonId: [null],
      assignmentId: [null],
      sourceCode: ["", Validators.required],
    });
  }

  ngOnInit(): void {
    const studentIdParam = this.route.snapshot.paramMap.get("studentId");
    const submissionIdParam = this.route.snapshot.paramMap.get("submissionId");

    if (studentIdParam) {
      this.studentId = parseInt(studentIdParam, 10);
    }

    if (submissionIdParam) {
      this.isEditMode = true;
      this.submissionId = parseInt(submissionIdParam, 10);
      this.loadSubmission(this.studentId, this.submissionId);
    } else {
      this.form.get("assignmentId")?.setValidators(Validators.required);
      this.form.get("assignmentId")?.updateValueAndValidity();
      this.loadLessons();
    }
  }

  loadLessons(): void {
    this.lessonsService.getAll().subscribe({
      next: (lessons: LessonResponseDto[]) => {
        this.lessons = lessons;
        this.lessonOptions = lessons.map((l) => ({
          label: l.subject ? `${l.courseName} — ${l.subject}` : l.courseName,
          value: l.id,
        }));
      },
      error: (_error: unknown) => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "טעינת השיעורים נכשלה",
        });
      },
    });
  }

  onLessonChange(): void {
    const lessonId = this.form.get("lessonId")?.value;
    if (lessonId) {
      this.form.patchValue({ assignmentId: null });
      this.assignmentsService.getByLesson(lessonId).subscribe({
        next: (assignments: AssignmentResponseDto[]) => {
          this.assignments = assignments;
          this.assignmentOptions = assignments.map(
            (a: AssignmentResponseDto) => ({
              label: a.title || "ללא שם",
              value: a.id,
            }),
          );
        },
        error: (_error: unknown) => {
          this.messageService.add({
            severity: "error",
            summary: "שגיאה",
            detail: "טעינת התרגילים נכשלה",
          });
        },
      });
    } else {
      this.assignmentOptions = [];
    }
  }

  loadSubmission(studentId: number, submissionId: number): void {
    this.loading = true;
    this.submissionsService.getById(studentId, submissionId).subscribe({
      next: (submission: SubmissionResponseDto) => {
        this.submission = submission;
        this.form.patchValue({
          sourceCode: submission.sourceCode,
        });
        this.loading = false;
      },
      error: (_error: unknown) => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "טעינת ההגשה נכשלה",
        });
        this.loading = false;
      },
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      return;
    }

    this.loading = true;
    const formValue = this.form.value;

    if (this.isEditMode) {
      const request: UpdateSubmissionRequestDto = {
        sourceCode: formValue.sourceCode,
        // מסך המורה עורך קוד יחיד בלבד; null משאיר את קבצי ההגשה הקיימים כפי שהם
        files: null,
      };

      this.submissionsService
        .update(this.studentId, this.submissionId!, request)
        .subscribe({
          next: () => {
            this.messageService.add({
              severity: "success",
              summary: "בוצע",
              detail: "ההגשה עודכנה בהצלחה",
            });
            this.router.navigate(["/students", this.studentId, "submissions"]);
          },
          error: (_error: unknown) => {
            this.messageService.add({
              severity: "error",
              summary: "שגיאה",
              detail: "עדכון ההגשה נכשל",
            });
            this.loading = false;
          },
        });
    } else {
      const request: CreateSubmissionRequestDto = {
        assignmentId: formValue.assignmentId,
        sourceCode: formValue.sourceCode,
        files: null,
      };

      this.submissionsService.create(this.studentId, request).subscribe({
        next: () => {
          this.messageService.add({
            severity: "success",
            summary: "בוצע",
            detail: "ההגשה נשלחה בהצלחה",
          });
          this.router.navigate(["/students", this.studentId, "submissions"]);
        },
        error: (_error: unknown) => {
          this.messageService.add({
            severity: "error",
            summary: "שגיאה",
            detail: "יצירת ההגשה נכשלה",
          });
          this.loading = false;
        },
      });
    }
  }

  onCancel(): void {
    this.router.navigate(["/students", this.studentId, "submissions"]);
  }
}
