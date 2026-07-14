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
  template: `
    <section class="sg-page">
      <div class="pt-3 pb-5">
        <p-card styleClass="sg-card sg-form-card">
          <ng-template pTemplate="header">
            <div
              class="flex flex-column md:flex-row md:align-items-end md:justify-content-between gap-3 px-4 pt-4 pb-2"
            >
              <div class="sg-title">
                <div class="sg-h1">
                  {{ isEditMode ? "עריכת הגשה" : "הגשה חדשה" }}
                </div>
                <div class="sg-h2">בחירת תרגיל והדבקת קוד</div>
              </div>
            </div>
          </ng-template>

          <form class="px-4 pb-4" [formGroup]="form" (ngSubmit)="onSubmit()">
            <div class="formgrid grid">
              <ng-container *ngIf="!isEditMode; else assignmentReadonly">
                <div class="field col-12 md:col-6">
                  <label class="block font-bold mb-2" for="lesson"
                    >שיעור *</label
                  >
                  <p-dropdown
                    inputId="lesson"
                    styleClass="w-full"
                    [options]="lessonOptions"
                    formControlName="lessonId"
                    placeholder="בחירת שיעור"
                    (onChange)="onLessonChange()"
                    optionLabel="label"
                    optionValue="value"
                    [showClear]="true"
                  >
                  </p-dropdown>
                </div>

                <div class="field col-12 md:col-6">
                  <label class="block font-bold mb-2" for="assignment"
                    >תרגיל *</label
                  >
                  <p-dropdown
                    inputId="assignment"
                    styleClass="w-full"
                    [options]="assignmentOptions"
                    formControlName="assignmentId"
                    placeholder="בחירת תרגיל"
                    optionLabel="label"
                    optionValue="value"
                    [disabled]="!form.get('lessonId')?.value"
                    [showClear]="true"
                  >
                  </p-dropdown>
                  <small
                    class="p-error"
                    *ngIf="
                      form.get('assignmentId')?.invalid &&
                      form.get('assignmentId')?.touched
                    "
                  >
                    יש לבחור תרגיל
                  </small>
                </div>
              </ng-container>

              <ng-template #assignmentReadonly>
                <div class="field col-12">
                  <label class="block font-bold mb-2">תרגיל</label>
                  <div class="sg-readonly-field">
                    {{ submission?.assignmentName || "—" }}
                  </div>
                </div>
              </ng-template>

              <div class="field col-12">
                <label class="block font-bold mb-2" for="sourceCode"
                  >קוד *</label
                >
                <textarea
                  pInputTextarea
                  id="sourceCode"
                  class="w-full sg-code-textarea"
                  formControlName="sourceCode"
                  rows="16"
                  placeholder="יש להדביק כאן קוד..."
                  (blur)="form.get('sourceCode')?.markAsTouched()"
                ></textarea>
                <small
                  class="p-error"
                  *ngIf="
                    form.get('sourceCode')?.invalid &&
                    form.get('sourceCode')?.touched
                  "
                >
                  יש להדביק קוד
                </small>
              </div>
            </div>

            <div class="sg-form-actions">
              <p-button
                label="ביטול"
                severity="secondary"
                [outlined]="true"
                (onClick)="onCancel()"
                type="button"
              ></p-button>
              <p-button
                [label]="isEditMode ? 'שמירה' : 'יצירה'"
                type="submit"
                styleClass="sg-btn-primary"
                [loading]="loading"
                [disabled]="form.invalid"
              ></p-button>
            </div>
          </form>
        </p-card>
      </div>
    </section>
  `,
  styles: [
    `
      .sg-code-textarea {
        font-family: "Consolas", "Courier New", monospace;
        direction: ltr;
        text-align: left;
        resize: vertical;
      }

      .sg-readonly-field {
        padding: 0.65rem 0.85rem;
        border-radius: var(--border-radius, 6px);
        background: rgba(0, 0, 0, 0.04);
        border: 1px solid var(--app-border, #d9d9d9);
        font-weight: 600;
      }
    `,
  ],
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
          label: l.name || "ללא שם",
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
