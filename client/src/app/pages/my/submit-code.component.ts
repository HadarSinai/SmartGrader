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
import { MessageModule } from "primeng/message";
import { PanelModule } from "primeng/panel";

import { AssignmentResponseDto, TestCaseDto } from "@models/assignment.model";
import {
  SubmissionFileDto,
  SubmissionResponseDto,
} from "@models/submission.model";
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
    MessageModule,
    PanelModule,
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
                {{ backLabel }}
              </a>
              <div class="sg-title mt-2">
                <div class="sg-h1">
                  {{ isEditMode ? "תיקון והגשה מחדש" : "הגשת קוד" }}
                </div>
                <div class="sg-h2" *ngIf="assignment">
                  {{ assignment.title }}
                  <span *ngIf="showMethodName">
                    · שם המתודה:
                    <code dir="ltr">{{ assignment.methodName }}</code>
                  </span>
                </div>
              </div>
            </div>
          </ng-template>

          <form [formGroup]="form" (ngSubmit)="onSubmit()" class="p-fluid">
            <div class="field" *ngIf="!isMultiFile">
              <label class="sg-label block" for="sourceCode">הקוד שלך *</label>
              <p-message
                severity="info"
                styleClass="w-full mb-2"
                [text]="submissionHint"
              ></p-message>
              <p-message
                *ngIf="showNoPrintingNote"
                severity="warn"
                styleClass="w-full mb-2"
                text="בתרגיל מסוג זה המערכת מדפיסה את הערך המוחזר — אל תדפיסי בעצמך."
              ></p-message>
              <textarea
                pInputTextarea
                id="sourceCode"
                formControlName="sourceCode"
                rows="16"
                class="sg-code-input w-full"
                dir="ltr"
                spellcheck="false"
                [placeholder]="computedPlaceholder"
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

              <p-panel
                *ngIf="exampleTest"
                header="דוגמה לפורמט טסט"
                [toggleable]="true"
                [collapsed]="true"
                styleClass="mt-2"
              >
                <div class="sg-example-test">
                  <div>
                    <strong>{{ inputLabel }}:</strong>
                    <code dir="ltr">{{ exampleTest.input }}</code>
                  </div>
                  <div>
                    <strong>פלט צפוי:</strong>
                    <code dir="ltr">{{ exampleTest.expected }}</code>
                  </div>
                </div>
              </p-panel>
            </div>

            <div class="field" *ngIf="isMultiFile">
              <label class="sg-label block">קבצי הקוד שלך *</label>
              <p-message
                severity="info"
                styleClass="w-full mb-2"
                [text]="multiFileHint"
              ></p-message>

              <div
                *ngFor="let expected of assignment?.expectedFiles"
                class="sg-file-slot flex align-items-center gap-3 p-3 mb-2 border-1 border-round-xl"
                style="border-color: var(--app-border)"
              >
                <input
                  type="file"
                  class="hidden"
                  #fileInput
                  (change)="onFileSelected(expected.fileName, $event)"
                />
                <p-button
                  type="button"
                  label="בחירת קובץ"
                  icon="pi pi-upload"
                  [outlined]="true"
                  styleClass="sg-btn-secondary"
                  (onClick)="fileInput.click()"
                ></p-button>
                <div class="flex flex-column">
                  <span class="font-bold" dir="ltr">{{
                    expected.fileName
                  }}</span>
                  <span
                    class="text-color-secondary text-sm"
                    *ngIf="expected.description"
                  >
                    {{ expected.description }}
                  </span>
                  <span
                    class="text-sm"
                    [class.text-green-600]="isFileLoaded(expected.fileName)"
                  >
                    {{
                      isFileLoaded(expected.fileName)
                        ? "✓ הקובץ נטען"
                        : "טרם נבחר קובץ"
                    }}
                  </span>
                </div>
              </div>

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
                [label]="isEditMode ? 'הגשה מחדש' : 'הגשה'"
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

      .sg-example-test {
        display: flex;
        flex-direction: column;
        gap: var(--space-2);
        font-size: var(--text-sm);
      }

      .sg-example-test code {
        direction: ltr;
        display: inline-block;
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

  /**
   * מסך אחד לשני המצבים: הגשה ראשונה (/my/lessons/:lessonId/assignments/:assignmentId/submit)
   * ותיקון והגשה מחדש (/my/submissions/:submissionId/edit). העורך, הוולידציה וטיפול הקבצים
   * זהים — רק המקור של הנתונים והקריאה בסוף שונים.
   */
  submissionId: number | null = null;

  private readonly loadedFiles = new Map<string, string>();

  readonly fullProgramPlaceholder =
    "using System;\n\nclass Program\n{\n    static void Main()\n    {\n        // כתבי כאן את התוכנית שלך: קריאת קלט עם Console.ReadLine()\n        // והדפסת התוצאה עם Console.WriteLine()\n    }\n}";
  readonly methodPlaceholder = "public int Sum(int a, int b)\n{\n    ...\n}";

  get isEditMode(): boolean {
    return this.submissionId !== null;
  }

  get backLink(): (string | number)[] {
    if (this.isEditMode) return ["/my", "submissions", this.submissionId!];
    if (!this.lessonId) return ["/my", "lessons"];
    return ["/my", "lessons", this.lessonId, "assignments"];
  }

  get backLabel(): string {
    if (this.isEditMode) return "חזרה לפידבק";
    if (!this.lessonId) return "חזרה לשיעורים שלי";
    return "חזרה לתרגילים";
  }

  /**
   * במצב "מתודה בודדת" העטיפה מדפיסה בעצמה את הערך המוחזר. הדפסה נוספת של התלמידה מוסיפה
   * שורה ל-stdout ומפילה את הבדיקה אף שהלוגיקה נכונה — ולכן זה נאמר לה מראש.
   */
  get showNoPrintingNote(): boolean {
    return this.gradingMode === "Method";
  }

  /** כל ההנחיות למטה נגזרות מ-gradingMode, כך שהתלמיד תמיד יודע איזו צורת קוד מצופה ממנו. */
  get gradingMode(): string {
    return this.assignment?.gradingMode ?? "Method";
  }

  get showMethodName(): boolean {
    return this.gradingMode !== "FullProgram" && !!this.assignment?.methodName;
  }

  get computedPlaceholder(): string {
    if (this.gradingMode === "FullProgram") return this.fullProgramPlaceholder;
    const methodName = this.assignment?.methodName;
    if (!methodName) return this.methodPlaceholder;
    return `public int ${methodName}(...)\n{\n    ...\n}`;
  }

  get submissionHint(): string {
    if (this.gradingMode === "FullProgram") {
      return "יש להגיש תוכנית שלמה — כולל using, class ו-Main. התוכנית קוראת קלט עם Console.ReadLine() ומדפיסה את הפלט עם Console.WriteLine().";
    }
    const methodName = this.assignment?.methodName;
    if (!methodName) {
      return "יש להגיש רק את גוף המתודה המבוקשת — בלי class, using או Main. הקוד נעטף אוטומטית בסביבת ההרצה.";
    }
    return `יש להגיש רק את גוף המתודה "${methodName}" — בלי class, using או Main. הקוד נעטף אוטומטית בסביבת ההרצה.`;
  }

  get multiFileHint(): string {
    if (this.gradingMode === "FullProgram") {
      return "תרגיל זה דורש הגשה של מספר קבצים. יש להעלות קובץ לכל שורה — התוכנית תורץ כמו שהיא, עם ה-Main שכתבת.";
    }
    const entryMethod = this.assignment?.expectedFiles?.find(
      (f) => f.methodName,
    )?.methodName;
    return entryMethod
      ? `תרגיל זה דורש הגשה של מספר קבצים. יש להעלות קובץ לכל שורה — הבדיקה תקרא למתודה "${entryMethod}".`
      : "תרגיל זה דורש הגשה של מספר קבצים. יש להעלות קובץ לכל שורה.";
  }

  get inputLabel(): string {
    return this.gradingMode === "FullProgram" ? "קלט (stdin)" : "קלט";
  }

  get exampleTest(): TestCaseDto | null {
    const tests = this.assignment?.tests;
    return tests && tests.length > 0 ? tests[0] : null;
  }

  get isMultiFile(): boolean {
    return !!this.assignment?.expectedFiles?.length;
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
    this.form = this.fb.group({
      sourceCode: ["", Validators.required],
    });

    const submissionIdParam = this.route.snapshot.paramMap.get("submissionId");
    if (submissionIdParam !== null) {
      this.submissionId = Number(submissionIdParam);
      this.loadExistingSubmission();
      return;
    }

    this.lessonId = Number(this.route.snapshot.paramMap.get("lessonId"));
    this.assignmentId = Number(
      this.route.snapshot.paramMap.get("assignmentId"),
    );
    this.loadAssignment();
  }

  /** טוען את ההגשה הקיימת לתוך אותו טופס, כדי שהתלמידה תתקן את הקוד שלה ולא תכתוב מחדש. */
  private loadExistingSubmission(): void {
    const studentId = this.auth.studentId();
    if (studentId === null) return;

    this.submissionsService.getById(studentId, this.submissionId!).subscribe({
      next: (submission: SubmissionResponseDto) => {
        this.assignmentId = submission.assignmentId;
        this.lessonId = submission.lessonId;
        this.form.patchValue({ sourceCode: submission.sourceCode ?? "" });

        for (const file of submission.sourceFiles ?? []) {
          this.loadedFiles.set(file.fileName, file.content);
        }

        this.loadAssignment();
      },
      error: () => {
        // Toast shown by ApiErrorInterceptor
      },
    });
  }

  private loadAssignment(): void {
    // בלי lessonId אין endpoint לתרגיל בודד — הטופס עדיין עובד, רק בלי ההנחיות שנגזרות ממנו
    if (!this.lessonId || !this.assignmentId) return;

    this.assignmentsService
      .getById(this.lessonId, this.assignmentId)
      .subscribe({
        next: (assignment: AssignmentResponseDto) => {
          this.assignment = assignment;
          if (this.isMultiFile) {
            this.form.get("sourceCode")?.clearValidators();
            this.form.get("sourceCode")?.updateValueAndValidity();
          }
        },
        error: () => {
          // Toast shown by ApiErrorInterceptor; stay on page with generic header
        },
      });
  }

  onFileSelected(fileName: string, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = () => {
      this.loadedFiles.set(fileName, String(reader.result ?? ""));
    };
    reader.readAsText(file);
  }

  isFileLoaded(fileName: string): boolean {
    return this.loadedFiles.has(fileName);
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    let files: SubmissionFileDto[] | null = null;
    if (this.isMultiFile) {
      const expectedFiles = this.assignment?.expectedFiles ?? [];
      const missing = expectedFiles.filter(
        (f) => !this.isFileLoaded(f.fileName),
      );
      if (missing.length > 0) {
        this.messageService.add({
          severity: "warn",
          summary: "חסרים קבצים",
          detail: `יש לבחור קובץ עבור: ${missing.map((f) => f.fileName).join(", ")}`,
        });
        return;
      }
      files = expectedFiles.map((f) => ({
        fileName: f.fileName,
        content: this.loadedFiles.get(f.fileName) ?? "",
      }));
    }

    // ⚠️ עד לתיקון הזה זו הייתה יציאה שקטה: כפתור ההגשה פשוט לא עשה כלום, בלי הודעה ובלי שגיאה.
    // המצב הזה נחסם עכשיו כבר בהתחברות וב-studentGuard, וזו שכבת ההגנה האחרונה.
    const studentId = this.auth.studentId();
    if (studentId === null) {
      this.messageService.add({
        severity: "error",
        summary: "לא ניתן להגיש",
        detail:
          "החשבון שלך אינו מקושר לרשומת תלמידה במערכת. יש לפנות למורה כדי לקשר אותו.",
        life: 8000,
      });
      return;
    }

    this.submitting = true;
    const sourceCode = this.isMultiFile ? "" : this.form.value.sourceCode;

    const request$ = this.isEditMode
      ? this.submissionsService.update(studentId, this.submissionId!, {
          sourceCode,
          files,
        })
      : this.submissionsService.create(studentId, {
          assignmentId: this.assignmentId,
          sourceCode,
          files,
        });

    request$.subscribe({
      next: (submission: SubmissionResponseDto) => {
        this.messageService.add({
          severity: "success",
          summary: "בוצע",
          detail: this.isEditMode
            ? "הקוד המתוקן נשלח לבדיקה"
            : "הקוד נשלח לבדיקה",
        });
        this.router.navigate(["/my", "submissions", submission.id], {
          queryParams: { lessonId: this.lessonId || null },
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
