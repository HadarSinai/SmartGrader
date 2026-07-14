import { CommonModule } from "@angular/common";
import { Component, OnInit } from "@angular/core";
import {
    FormBuilder,
    FormGroup,
    ReactiveFormsModule,
    Validators,
} from "@angular/forms";
import { ActivatedRoute, Router, RouterModule } from "@angular/router";

import { ConfirmationService, MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { ConfirmDialogModule } from "primeng/confirmdialog";
import { InputTextareaModule } from "primeng/inputtextarea";

import { AssignmentResponseDto } from "@models/assignment.model";
import { SubmissionResponseDto } from "@models/submission.model";
import { AssignmentsService } from "@services/assignments.service";
import { AuthService } from "@services/auth.service";
import { SubmissionsService } from "@services/submissions.service";

@Component({
  selector: "app-submit-code",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    ButtonModule,
    CardModule,
    ConfirmDialogModule,
    InputTextareaModule,
  ],
  providers: [ConfirmationService],
  template: `
    <p-confirmDialog></p-confirmDialog>

    <section class="sg-page">
      <div class="pt-3 pb-5">
        <p-card styleClass="sg-card sg-form-card">
          <ng-template pTemplate="header">
            <div class="px-4 pt-4 pb-2">
              <a class="sg-breadcrumb-link" [routerLink]="backLink">
                <i class="pi pi-arrow-right" aria-hidden="true"></i>
                חזרה לתרגילים
              </a>
              <div class="sg-title mt-2">
                <div class="sg-h1">הגשת קוד</div>
                <div class="sg-h2" *ngIf="assignment">
                  {{ assignment.title }}
                  <span *ngIf="assignment.methodName">
                    · שם המתודה:
                    <code dir="ltr">{{ assignment.methodName }}</code>
                  </span>
                </div>
              </div>
            </div>
          </ng-template>

          <form [formGroup]="form" (ngSubmit)="onSubmit()" class="p-fluid">
            <div class="field">
              <label class="sg-label block" for="sourceCode">הקוד שלך *</label>
              <textarea
                pInputTextarea
                id="sourceCode"
                formControlName="sourceCode"
                rows="16"
                class="sg-code-input w-full"
                dir="ltr"
                spellcheck="false"
                [placeholder]="codePlaceholder"
              ></textarea>
              <small
                class="p-error"
                *ngIf="
                  form.get('sourceCode')?.invalid &&
                  form.get('sourceCode')?.touched
                "
              >
                הקוד הוא שדה חובה
              </small>
              <small class="sg-hint block mt-1">
                לאחר ההגשה הקוד ייבדק אוטומטית ותתקבל התראה עם הפידבק.
              </small>
            </div>

            <div class="sg-form-actions">
              <p-button
                type="button"
                label="ביטול"
                styleClass="sg-btn-secondary"
                [outlined]="true"
                (onClick)="onCancel()"
              ></p-button>
              <p-button
                type="submit"
                label="הגשה"
                icon="pi pi-upload"
                styleClass="sg-btn-primary"
                [loading]="submitting"
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
      .sg-code-input {
        font-family:
          "Cascadia Code", "Fira Code", Consolas, "Courier New", monospace;
        font-size: 0.9rem;
        direction: ltr;
        text-align: left;
      }
    `,
  ],
})
export class SubmitCodeComponent implements OnInit {
  form!: FormGroup;
  lessonId!: number;
  assignmentId!: number;
  assignment: AssignmentResponseDto | null = null;
  submitting = false;

  readonly codePlaceholder = "public int Sum(int a, int b)\n{\n    ...\n}";

  get backLink(): (string | number)[] {
    return ["/my", "lessons", this.lessonId, "assignments"];
  }

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private assignmentsService: AssignmentsService,
    private submissionsService: SubmissionsService,
    private auth: AuthService,
    private messageService: MessageService,
    private confirmationService: ConfirmationService,
  ) {}

  ngOnInit(): void {
    this.lessonId = Number(this.route.snapshot.paramMap.get("lessonId"));
    this.assignmentId = Number(
      this.route.snapshot.paramMap.get("assignmentId"),
    );

    this.form = this.fb.group({
      sourceCode: ["", Validators.required],
    });

    this.assignmentsService
      .getById(this.lessonId, this.assignmentId)
      .subscribe({
        next: (assignment: AssignmentResponseDto) => {
          this.assignment = assignment;
        },
        error: () => {
          // Toast shown by ApiErrorInterceptor; stay on page with generic header
        },
      });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const studentId = this.auth.studentId();
    if (studentId === null) return;

    this.submitting = true;
    this.submissionsService
      .create(studentId, {
        assignmentId: this.assignmentId,
        sourceCode: this.form.value.sourceCode,
      })
      .subscribe({
        next: (submission: SubmissionResponseDto) => {
          this.messageService.add({
            severity: "success",
            summary: "בוצע",
            detail: "הקוד נשלח לבדיקה",
          });
          this.router.navigate(["/my", "submissions", submission.id], {
            queryParams: { lessonId: this.lessonId },
          });
        },
        error: () => {
          this.submitting = false;
        },
      });
  }

  onCancel(): void {
    if (this.form.dirty) {
      this.confirmationService.confirm({
        message: "יש שינויים שלא נשמרו. לצאת בכל זאת?",
        header: "שינויים שלא נשמרו",
        icon: "pi pi-exclamation-triangle",
        acceptLabel: "יציאה",
        rejectLabel: "ביטול",
        accept: () => this.router.navigate(this.backLink),
      });
      return;
    }
    this.router.navigate(this.backLink);
  }
}
