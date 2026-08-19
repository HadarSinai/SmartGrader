import { CommonModule } from "@angular/common";
import { Component, OnInit, inject } from "@angular/core";
import {
  FormArray,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";
import {
  AssignmentResponseDto,
  CreateAssignmentRequestDto,
  ExpectedFileDto,
  GRADING_MODE_LABELS_HE,
  GradingMode,
  TestCaseDto,
  UpdateAssignmentRequestDto,
} from "@models/assignment.model";
import { AssignmentsService } from "@services/assignments.service";
import { ConfirmationService, MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { CheckboxModule } from "primeng/checkbox";
import { ConfirmDialogModule } from "primeng/confirmdialog";
import { InputNumberModule } from "primeng/inputnumber";
import { InputTextModule } from "primeng/inputtext";
import { InputTextareaModule } from "primeng/inputtextarea";
import { MessageModule } from "primeng/message";
import { SelectButtonModule } from "primeng/selectbutton";

@Component({
  selector: "app-assignment-form",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CardModule,
    InputTextModule,
    InputTextareaModule,
    InputNumberModule,
    CheckboxModule,
    ButtonModule,
    ConfirmDialogModule,
    MessageModule,
    SelectButtonModule,
  ],
  providers: [ConfirmationService],
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
                  {{ isEditMode ? "עריכת תרגיל" : "תרגיל חדש" }}
                </div>
                <div class="sg-h2">הגדרת תרגיל ומקרי בדיקה</div>
              </div>
            </div>
          </ng-template>

          <form class="px-4 pb-4" [formGroup]="form" (ngSubmit)="onSubmit()">
            <div class="formgrid grid">
              <div class="field col-12 md:col-6">
                <label class="block font-bold mb-2" for="title">כותרת *</label>
                <input
                  pInputText
                  class="w-full"
                  id="title"
                  formControlName="title"
                  placeholder="לדוגמה: מיון מערכים"
                />
                <small
                  class="p-error"
                  *ngIf="
                    form.get('title')?.invalid && form.get('title')?.touched
                  "
                >
                  כותרת היא שדה חובה
                </small>
              </div>

              <div class="field col-12 md:col-6">
                <label class="block font-bold mb-2" for="bonusValue"
                  >בונוס</label
                >
                <div class="flex align-items-center gap-3 flex-wrap">
                  <div class="flex align-items-center gap-2">
                    <p-checkbox
                      inputId="isBonus"
                      formControlName="isBonus"
                      [binary]="true"
                    ></p-checkbox>
                    <label for="isBonus" class="font-semibold"
                      >תרגיל בונוס</label
                    >
                  </div>

                  <p-inputNumber
                    *ngIf="form.get('isBonus')?.value"
                    inputId="bonusValue"
                    styleClass="w-full"
                    formControlName="bonusValue"
                    [minFractionDigits]="0"
                    [maxFractionDigits]="2"
                  >
                  </p-inputNumber>
                </div>
              </div>

              <div class="field col-12">
                <label class="block font-bold mb-2" for="description"
                  >תיאור</label
                >
                <textarea
                  pInputTextarea
                  class="w-full"
                  id="description"
                  formControlName="description"
                  rows="4"
                  placeholder="הסבר קצר לסטודנטים"
                ></textarea>
              </div>

              <div class="field col-12">
                <label class="block font-bold mb-2">סוג ההגשה *</label>
                <p-selectButton
                  [options]="gradingModeOptions"
                  formControlName="gradingMode"
                  optionLabel="label"
                  optionValue="value"
                ></p-selectButton>
                <small class="sg-hint block mt-2">{{
                  gradingModeDescription
                }}</small>
              </div>

              <div class="field col-12 md:col-6" *ngIf="isMethodMode">
                <label class="block font-bold mb-2" for="methodName"
                  >שם המתודה *</label
                >
                <input
                  pInputText
                  class="w-full"
                  id="methodName"
                  formControlName="methodName"
                  placeholder="לדוגמה: Sum"
                />
                <small
                  class="p-error"
                  *ngIf="
                    form.get('methodName')?.invalid &&
                    form.get('methodName')?.touched
                  "
                >
                  שם המתודה הוא שדה חובה
                </small>
              </div>

              <div class="field col-12">
                <div
                  class="flex align-items-center justify-content-between gap-2 flex-wrap"
                >
                  <div class="font-bold">
                    קבצים נדרשים (הגשה רב-קובצית){{
                      isFullProgramMode ? " — אופציונלי" : " *"
                    }}
                  </div>
                  <p-button
                    label="הוספת קובץ"
                    icon="pi pi-plus"
                    [text]="true"
                    (onClick)="addExpectedFile()"
                    type="button"
                  ></p-button>
                </div>
                <p-message
                  *ngIf="isFullProgramMode"
                  severity="info"
                  styleClass="w-full mt-2"
                  text="אם מוגדרים קבצים, התלמיד יעלה קובץ לכל שורה והתוכנית תורץ עם ה-Main שהוא כתב — בלי עטיפה."
                ></p-message>

                <div
                  formArrayName="expectedFiles"
                  class="mt-3 flex flex-column gap-3"
                >
                  <div
                    *ngFor="let file of expectedFiles.controls; let i = index"
                    [formGroupName]="i"
                    class="p-3 border-1 border-round-xl"
                    style="border-color: var(--app-border); background: rgba(239,232,221,0.40)"
                  >
                    <div
                      class="flex align-items-center justify-content-between gap-2 flex-wrap mb-3"
                    >
                      <div class="font-bold text-color">קובץ {{ i + 1 }}</div>
                      <p-button
                        icon="pi pi-trash"
                        severity="danger"
                        [text]="true"
                        (onClick)="removeExpectedFile(i)"
                        type="button"
                      ></p-button>
                    </div>

                    <div class="grid">
                      <div class="col-12 md:col-4">
                        <label class="block font-bold mb-2">שם קובץ</label>
                        <input
                          pInputText
                          class="w-full"
                          formControlName="fileName"
                          placeholder="לדוגמה: Calculator.cs"
                        />
                      </div>
                      <div class="col-12 md:col-4" *ngIf="!isFullProgramMode">
                        <label class="block font-bold mb-2"
                          >שם המתודה בקובץ</label
                        >
                        <input
                          pInputText
                          class="w-full"
                          formControlName="methodName"
                          placeholder="לדוגמה: Sum"
                        />
                      </div>
                      <div class="col-12 md:col-4">
                        <label class="block font-bold mb-2">תיאור</label>
                        <input
                          pInputText
                          class="w-full"
                          formControlName="description"
                          placeholder="הסבר קצר לקובץ"
                        />
                      </div>
                    </div>
                  </div>

                  <div
                    *ngIf="expectedFiles.length === 0"
                    class="text-color-secondary p-3 border-1 border-round-xl"
                    style="border-color: var(--app-border);"
                  >
                    {{
                      isFullProgramMode
                        ? "ללא קבצים נדרשים — התלמיד יגיש קובץ יחיד עם Main."
                        : "ללא קבצים נדרשים — הגשה תישאר בפורמט הישן (קובץ יחיד)."
                    }}
                  </div>
                </div>
              </div>

              <div class="field col-12">
                <div
                  class="flex align-items-center justify-content-between gap-2 flex-wrap"
                >
                  <div class="font-bold">מקרי בדיקה</div>
                  <p-button
                    label="הוספת מקרה"
                    icon="pi pi-plus"
                    [text]="true"
                    (onClick)="addTestCase()"
                    type="button"
                  ></p-button>
                </div>
                <p-message
                  severity="info"
                  styleClass="w-full mt-2"
                  [text]="testCaseInputHint"
                ></p-message>

                <!-- מקרה שאינו דוגמה מוסתר לחלוטין מהתלמידה (גם הקלט וגם הפלט הצפוי), לכן
                     בלי אף דוגמה היא לא יכולה לדעת באיזה פורמט הקלט מגיע. אזהרה רכה בלבד —
                     ההגשה עצמה לא נחסמת. -->
                <p-message
                  *ngIf="tests.length > 0 && !hasSampleTest"
                  severity="warn"
                  styleClass="w-full mt-2"
                  text="אף מקרה בדיקה לא סומן כדוגמה — התלמידה לא תראה אף קלט לדוגמה ולא תדע באיזה פורמט להגיש. מומלץ לסמן לפחות אחד."
                ></p-message>

                <div formArrayName="tests" class="mt-3 flex flex-column gap-3">
                  <div
                    *ngFor="let test of tests.controls; let i = index"
                    [formGroupName]="i"
                    class="p-3 border-1 border-round-xl"
                    style="border-color: var(--app-border); background: rgba(239,232,221,0.40)"
                  >
                    <div
                      class="flex align-items-center justify-content-between gap-2 flex-wrap mb-3"
                    >
                      <div class="font-bold text-color">מקרה {{ i + 1 }}</div>
                      <p-button
                        icon="pi pi-trash"
                        severity="danger"
                        [text]="true"
                        (onClick)="removeTestCase(i)"
                        type="button"
                      ></p-button>
                    </div>

                    <div class="grid">
                      <div class="col-12 md:col-6">
                        <label class="block font-bold mb-2">קלט</label>
                        <textarea
                          pInputTextarea
                          class="w-full"
                          formControlName="input"
                          rows="2"
                          placeholder="הקלידי קלט"
                        ></textarea>
                      </div>
                      <div class="col-12 md:col-6">
                        <label class="block font-bold mb-2">פלט צפוי</label>
                        <textarea
                          pInputTextarea
                          class="w-full"
                          formControlName="expected"
                          rows="2"
                          placeholder="הקלידי פלט צפוי"
                        ></textarea>
                      </div>

                      <!-- שורת הדגלים של המקרה. מוגדרת כרשימה של דגלים ולא כדגל בודד כדי
                           שתוספת דגל נוסף בהמשך (למשל מקרה ליבה מול מקרה קצה, שמשפיע על
                           הניקוד) תהיה הוספת בלוק כאן ולא שינוי פריסה. -->
                      <div class="col-12">
                        <div class="sg-test-flags">
                          <div class="sg-test-flag">
                            <p-checkbox
                              [inputId]="'isSample' + i"
                              formControlName="isSample"
                              [binary]="true"
                            ></p-checkbox>
                            <label [for]="'isSample' + i">
                              מקרה דוגמה — מוצג לתלמידה
                            </label>
                          </div>
                        </div>
                        <small class="sg-hint block mt-1">
                          מקרה שאינו דוגמה לא נשלח לתלמידה כלל — לא לפני ההגשה ולא אחרי
                          הבדיקה. היא רואה רק אם הוא עבר או נכשל.
                        </small>
                      </div>
                    </div>
                  </div>

                  <div
                    *ngIf="tests.length === 0"
                    class="text-color-secondary p-3 border-1 border-round-xl"
                    style="border-color: var(--app-border);"
                  >
                    עדיין אין מקרי בדיקה. מומלץ להוסיף לפחות אחד.
                  </div>
                </div>
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

    <p-confirmDialog></p-confirmDialog>
  `,
  styles: [
    `
      .sg-test-flags {
        display: flex;
        flex-wrap: wrap;
        gap: var(--space-4);
      }

      .sg-test-flag {
        display: flex;
        align-items: center;
        gap: var(--space-2);
      }

      .sg-test-flag label {
        font-weight: 600;
        cursor: pointer;
      }
    `,
  ],
})
export class AssignmentFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly assignmentsService = inject(AssignmentsService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  form: FormGroup;
  loading = false;
  isEditMode = false;
  lessonId!: number;
  assignmentId: number | null = null;

  readonly gradingModeOptions: { label: string; value: GradingMode }[] =
    Object.entries(GRADING_MODE_LABELS_HE).map(([value, label]) => ({
      label,
      value: value as GradingMode,
    }));

  readonly gradingModeDescriptions: Record<GradingMode, string> = {
    FullProgram:
      "התלמיד מגיש תוכנית שלמה כולל using/class/Main משלו. מתאים לתחילת הקורס (לולאות, מערכים, מטריצות).",
    Method:
      "התלמיד מגיש רק גוף מתודה בודדת, בלי class/using/Main — הקוד נעטף אוטומטית בסביבת ההרצה.",
    MultiFileMethod:
      "התלמיד מגיש כמה קבצים (למשל מחלקות), ונבדקת קריאה למתודת כניסה מוגדרת — בלי Main של התלמיד.",
  };

  constructor() {
    this.form = this.fb.group({
      title: ["", Validators.required],
      description: [""],
      methodName: [""],
      gradingMode: ["FullProgram" as GradingMode, Validators.required],
      isBonus: [false],
      bonusValue: [0],
      tests: this.fb.array([]),
      expectedFiles: this.fb.array([]),
    });

    // MethodName נדרש רק במצב "מתודה בודדת" — שאר המצבים לא תלויים בו.
    this.form.get("gradingMode")?.valueChanges.subscribe(() => {
      this.updateMethodNameValidator();
    });
    this.updateMethodNameValidator();
  }

  get tests(): FormArray {
    return this.form.get("tests") as FormArray;
  }

  get expectedFiles(): FormArray {
    return this.form.get("expectedFiles") as FormArray;
  }

  get gradingMode(): GradingMode {
    return this.form.get("gradingMode")?.value as GradingMode;
  }

  get isMethodMode(): boolean {
    return this.gradingMode === "Method";
  }

  get isFullProgramMode(): boolean {
    return this.gradingMode === "FullProgram";
  }

  get gradingModeDescription(): string {
    return this.gradingModeDescriptions[this.gradingMode] ?? "";
  }

  get testCaseInputHint(): string {
    switch (this.gradingMode) {
      case "FullProgram":
        return "קלט (Input) הוא stdin מלא לתוכנית — כל שורה נקראת ב-Console.ReadLine() אחד, בדיוק כפי שהתלמיד יקליד בהרצה רגילה.";
      case "MultiFileMethod":
        return "קלט (Input) הוא מערך JSON של ארגומנטים למתודת הכניסה, למשל: [3, 5]";
      default:
        return "קלט (Input) הוא ערכי הפרמטרים של המתודה מופרדים ברווח, למשל: 3 5";
    }
  }

  private updateMethodNameValidator(): void {
    const control = this.form.get("methodName");
    if (!control) return;

    if (this.isMethodMode) {
      control.setValidators([Validators.required]);
    } else {
      control.clearValidators();
    }
    control.updateValueAndValidity({ emitEvent: false });
  }

  ngOnInit(): void {
    const lessonIdParam = this.route.snapshot.paramMap.get("lessonId");
    const assignmentIdParam = this.route.snapshot.paramMap.get("assignmentId");

    if (lessonIdParam) {
      this.lessonId = parseInt(lessonIdParam, 10);
    }

    if (assignmentIdParam) {
      this.isEditMode = true;
      this.assignmentId = parseInt(assignmentIdParam, 10);
      this.loadAssignment(this.lessonId, this.assignmentId);
    }
  }

  loadAssignment(lessonId: number, assignmentId: number): void {
    this.loading = true;
    this.assignmentsService.getById(lessonId, assignmentId).subscribe({
      next: (assignment: AssignmentResponseDto) => {
        this.form.patchValue({
          title: assignment.title,
          description: assignment.description,
          methodName: assignment.methodName,
          gradingMode: assignment.gradingMode,
          isBonus: assignment.isBonus,
          bonusValue: assignment.bonusValue,
        });

        if (assignment.tests) {
          assignment.tests.forEach((test) => {
            this.tests.push(this.createTestCaseGroup(test));
          });
        }

        if (assignment.expectedFiles) {
          assignment.expectedFiles.forEach((file) => {
            this.expectedFiles.push(this.createExpectedFileGroup(file));
          });
        }

        this.loading = false;
      },
      error: (_error: unknown) => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "טעינת התרגיל נכשלה",
        });
        this.loading = false;
      },
    });
  }

  createTestCaseGroup(testCase?: TestCaseDto): FormGroup {
    return this.fb.group({
      input: [testCase?.input || ""],
      expected: [testCase?.expected || ""],
      isSample: [testCase?.isSample ?? false],
    });
  }

  /** האם קיים לפחות מקרה דוגמה אחד — בסיס לאזהרה הרכה בטופס. */
  get hasSampleTest(): boolean {
    return this.tests.controls.some((c) => c.get("isSample")?.value === true);
  }

  addTestCase(): void {
    // המקרה הראשון מסומן כדוגמה כברירת מחדל: תרגיל בלי אף דוגמה משאיר את התלמידה בלי
    // שום רמז לפורמט הקלט. כל מקרה נוסף מוסתר כברירת מחדל (fail closed).
    this.tests.push(this.createTestCaseGroup({ input: "", expected: "", isSample: this.tests.length === 0 }));
  }

  removeTestCase(index: number): void {
    this.tests.removeAt(index);
  }

  createExpectedFileGroup(file?: ExpectedFileDto): FormGroup {
    return this.fb.group({
      fileName: [file?.fileName || "", Validators.required],
      methodName: [file?.methodName || ""],
      description: [file?.description || ""],
    });
  }

  addExpectedFile(): void {
    this.expectedFiles.push(this.createExpectedFileGroup());
  }

  removeExpectedFile(index: number): void {
    this.expectedFiles.removeAt(index);
  }

  onSubmit(): void {
    if (this.form.invalid) {
      return;
    }

    this.loading = true;
    const formValue = this.form.value;
    const request = {
      title: formValue.title,
      description: formValue.description,
      methodName: formValue.methodName,
      gradingMode: formValue.gradingMode,
      isBonus: formValue.isBonus,
      bonusValue: formValue.bonusValue,
      tests: formValue.tests,
      expectedFiles: formValue.expectedFiles,
    };

    const operation = this.isEditMode
      ? this.assignmentsService.update(
          this.lessonId,
          this.assignmentId!,
          request as UpdateAssignmentRequestDto,
        )
      : this.assignmentsService.create(
          this.lessonId,
          request as CreateAssignmentRequestDto,
        );

    operation.subscribe({
      next: () => {
        this.messageService.add({
          severity: "success",
          summary: "בוצע",
          detail: this.isEditMode
            ? "התרגיל עודכן בהצלחה"
            : "התרגיל נוצר בהצלחה",
        });
        this.router.navigate(["/lessons", this.lessonId, "assignments"]);
      },
      error: (_error: unknown) => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: this.isEditMode ? "עדכון התרגיל נכשל" : "יצירת התרגיל נכשלה",
        });
        this.loading = false;
      },
    });
  }

  onCancel(): void {
    if (this.form.dirty) {
      this.confirmationService.confirm({
        message: "יש לך שינויים שלא נשמרו. לצאת בכל זאת?",
        header: "שינויים שלא נשמרו",
        icon: "pi pi-exclamation-triangle",
        acceptLabel: "יציאה",
        rejectLabel: "ביטול",
        accept: () =>
          this.router.navigate(["/lessons", this.lessonId, "assignments"]),
      });
      return;
    }
    this.router.navigate(["/lessons", this.lessonId, "assignments"]);
  }
}
