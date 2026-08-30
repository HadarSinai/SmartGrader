import { CommonModule } from "@angular/common";
import { Component, OnInit, inject } from "@angular/core";
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";
import {
  SubmissionResponseDto,
  UpdateSubmissionRequestDto,
} from "@models/submission.model";
import { SubmissionsService } from "@services/submissions.service";
import { MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { InputTextareaModule } from "primeng/inputtextarea";

/**
 * עריכת הגשה קיימת, ורק היא.
 *
 * הרכיב הזה החזיק גם ענף יצירה — שני dropdown לבחירת שיעור ותרגיל, ואחריהם
 * POST — אבל לא היה לו מסלול: `students/:studentId/submissions/new` מעולם לא
 * נרשם, ואף מסך לא ניווט לשם. תלמידה מגישה דרך `/my/lessons/:lessonId/assignments/
 * :assignmentId/submit`, וזה המסך שנתחזק. מסך שאי אפשר להגיע אליו אינו תכונה
 * שממתינה למסלול; הוא קוד שנקרא בטעות כאילו הוא עובד.
 */
@Component({
  selector: "app-submission-form",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CardModule,
    ButtonModule,
    InputTextareaModule,
  ],
  templateUrl: "./submission-form.component.html",
  styleUrls: ["./submission-form.component.css"],
})
export class SubmissionFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly submissionsService = inject(SubmissionsService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly messageService = inject(MessageService);

  form: FormGroup;
  loading = false;
  studentId!: number;
  submissionId!: number;
  submission: SubmissionResponseDto | null = null;

  constructor() {
    this.form = this.fb.group({
      sourceCode: ["", Validators.required],
    });
  }

  ngOnInit(): void {
    this.studentId = Number(this.route.snapshot.paramMap.get("studentId"));
    this.submissionId = Number(this.route.snapshot.paramMap.get("submissionId"));

    this.loadSubmission(this.studentId, this.submissionId);
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

    const request: UpdateSubmissionRequestDto = {
      sourceCode: this.form.value.sourceCode,
      // מסך המורה עורך קוד יחיד בלבד; null משאיר את קבצי ההגשה הקיימים כפי שהם
      files: null,
    };

    this.submissionsService
      .update(this.studentId, this.submissionId, request)
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
  }

  onCancel(): void {
    this.router.navigate(["/students", this.studentId, "submissions"]);
  }
}
