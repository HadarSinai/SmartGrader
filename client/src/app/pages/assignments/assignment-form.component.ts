import { CommonModule } from "@angular/common";
import { Component, OnInit, inject } from "@angular/core";
import {
  AbstractControl,
  FormArray,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";
import {
  AssignmentResponseDto,
  CODE_CONSTRUCT_GROUPS,
  CODE_CONSTRUCT_LABELS_HE,
  CodeConstruct,
  CreateAssignmentRequestDto,
  DEFAULT_RETRY_THRESHOLD,
  ExpectedFileDto,
  GRADING_MODE_LABELS_HE,
  GradingMode,
  PAIRED_CONSTRUCT,
  RULE_KIND_LABELS_HE,
  RULE_SEVERITY_HINTS_HE,
  RULE_SEVERITY_LABELS_HE,
  ReferenceSolutionFileDto,
  RuleKind,
  RuleSeverity,
  StructuralRuleDto,
  SuggestTestCasesResultDto,
  SuggestedTestCaseDto,
  TOTAL_POINTS,
  TestCaseDto,
  UpdateAssignmentRequestDto,
  VerifyTestCasesResultDto,
  describeRule,
  hasValidRubric,
  isGradeable,
  maxScoreOf,
  scoredRulePoints,
} from "@models/assignment.model";
import { AssignmentsService } from "@services/assignments.service";
import { ConfirmationService, MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { CheckboxModule } from "primeng/checkbox";
import { ConfirmDialogModule } from "primeng/confirmdialog";
import { DropdownModule } from "primeng/dropdown";
import { InputNumberModule } from "primeng/inputnumber";
import { InputTextModule } from "primeng/inputtext";
import { InputTextareaModule } from "primeng/inputtextarea";
import { MessageModule } from "primeng/message";
import { PanelModule } from "primeng/panel";
import { SelectButtonModule } from "primeng/selectbutton";
import { TooltipModule } from "primeng/tooltip";
import { TestSuggestionsDialogComponent } from "./test-suggestions-dialog.component";
import { TestVerificationPanelComponent } from "./test-verification-panel.component";

/**
 * שיקוף של AssignmentGradeability בשרת, ברמת הטופס כולו.
 *
 * ⚠️ חייב להישאר זהה לו: ולידציה שנוטה ממנו אפילו במקרה קצה אחד מייצרת 400 שהמורה אינה
 * יכולה להסביר לעצמה, אחרי שהטופס אמר לה שהכול תקין.
 *
 * מחוץ למחלקה בכוונה — הוא נקרא מתוך הבנאי, ופונקציה ברמת המודול אינה תלויה בסדר
 * אתחול השדות של המחלקה.
 */
function gradeabilityValidator(
  control: AbstractControl,
): ValidationErrors | null {
  const rules = (control.get("structuralRules")?.value ??
    []) as StructuralRuleDto[];
  const testsCount = ((control.get("tests")?.value ?? []) as unknown[]).length;

  const errors: ValidationErrors = {};

  // "או", לא "וגם": תרגיל מחלקות מנוקד על המבנה בלבד.
  if (!isGradeable(testsCount, rules)) errors["notGradeable"] = true;

  const maxScore = maxScoreOf(
    !!control.get("isBonus")?.value,
    Number(control.get("bonusValue")?.value ?? 0),
  );

  if (
    !hasValidRubric(
      maxScore,
      Number(control.get("testsAllocation")?.value ?? 0),
      testsCount,
      rules,
    )
  ) {
    errors["rubric"] = true;
  }

  // דרישה מנוקדת בלי נקודות אינה עושה כלום — כנראה נשכח למלא את השדה.
  if (rules.some((r) => r.severity === "Scored" && (r.points ?? 0) < 1)) {
    errors["scoredPoints"] = true;
  }

  return Object.keys(errors).length > 0 ? errors : null;
}

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
    DropdownModule,
    MessageModule,
    PanelModule,
    SelectButtonModule,
    TooltipModule,
    TestVerificationPanelComponent,
    TestSuggestionsDialogComponent,
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

              <!-- הפתרון לדוגמה — מקופל כברירת מחדל: הוא הבסיס לשתי התכונות אבל אינו
                   שדה שממלאים בכל תרגיל, ופתוח כברירת מחדל הוא היה דוחף את מקרי הבדיקה
                   אל מתחת לקיפול. -->
              <div class="field col-12">
                <!-- ⚠️ נכס נפרד ולא [collapsed]="!hasReferenceSolution": ביטוי שנגזר מהטופס
                     נדרס בכל שינוי, וכשהמורה מוחקת את תוכן הפתרון הפאנל היה נסגר לה
                     באמצע העבודה. הערך נקבע פעם אחת, אחרי הטעינה. -->
                <p-panel
                  [toggleable]="true"
                  [collapsed]="referencePanelCollapsed"
                  styleClass="sg-ref-panel"
                >
                  <ng-template pTemplate="header">
                    <div class="flex align-items-center gap-2">
                      <i class="pi pi-lock"></i>
                      <span class="font-bold">פתרון לדוגמה (לא נראה לתלמידות)</span>
                    </div>
                  </ng-template>

                  <p class="sg-hint mt-0">
                    הפתרון התקין שלך לתרגיל. המערכת מריצה אותו מול מקרי הבדיקה כדי לוודא
                    שהפלטים הצפויים נכונים, ומשתמשת בו כדי לבדוק הצעות של ה-AI. הוא נשמר
                    בשרת ולעולם לא נשלח לתלמידה.
                  </p>

                  <p class="sg-hint mt-0">{{ referenceSolutionHint }}</p>

                  <div
                    class="flex align-items-center justify-content-end gap-2 flex-wrap"
                  >
                    <p-button
                      label="הוספת קובץ"
                      icon="pi pi-plus"
                      [text]="true"
                      (onClick)="addReferenceFile()"
                      type="button"
                    ></p-button>
                  </div>

                  <div
                    formArrayName="referenceSolution"
                    class="mt-2 flex flex-column gap-3"
                  >
                    <div
                      *ngFor="
                        let file of referenceSolution.controls;
                        let i = index
                      "
                      [formGroupName]="i"
                      class="p-3 border-1 border-round-xl"
                      style="border-color: var(--app-border);"
                    >
                      <div
                        class="flex align-items-center justify-content-between gap-2 flex-wrap mb-2"
                      >
                        <input
                          pInputText
                          class="sg-ref-filename"
                          formControlName="fileName"
                          placeholder="שם קובץ (לדוגמה: Solution.cs)"
                        />
                        <p-button
                          icon="pi pi-trash"
                          severity="danger"
                          [text]="true"
                          (onClick)="removeReferenceFile(i)"
                          type="button"
                        ></p-button>
                      </div>

                      <textarea
                        pInputTextarea
                        class="w-full sg-code-input"
                        formControlName="content"
                        rows="10"
                        [placeholder]="referenceSolutionHint"
                      ></textarea>
                    </div>

                    <div
                      *ngIf="referenceSolution.length === 0"
                      class="text-color-secondary p-3 border-1 border-round-xl"
                      style="border-color: var(--app-border);"
                    >
                      ללא פתרון לדוגמה — אפשר לשמור כך, אבל בלעדיו אי אפשר לבדוק את מקרי
                      הבדיקה ואי אפשר לאמת הצעות של ה-AI.
                    </div>
                  </div>
                </p-panel>
              </div>

              <div class="field col-12">
                <div
                  class="flex align-items-center justify-content-between gap-2 flex-wrap"
                >
                  <div class="font-bold">מקרי בדיקה</div>
                  <div class="flex align-items-center gap-1 flex-wrap">
                    <p-button
                      label="הצע מקרי בדיקה"
                      icon="pi pi-sparkles"
                      [text]="true"
                      [disabled]="!canSuggest"
                      [pTooltip]="suggestTooltip"
                      tooltipPosition="bottom"
                      (onClick)="suggestTests()"
                      type="button"
                    ></p-button>
                    <p-button
                      label="בדיקת מקרי הבדיקה"
                      icon="pi pi-play"
                      [text]="true"
                      [loading]="verifying"
                      [disabled]="!canVerify"
                      [pTooltip]="verifyTooltip"
                      tooltipPosition="bottom"
                      (onClick)="verifyTests()"
                      type="button"
                    ></p-button>
                    <p-button
                      label="הוספת מקרה"
                      icon="pi pi-plus"
                      [text]="true"
                      (onClick)="addTestCase()"
                      type="button"
                    ></p-button>
                  </div>
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

                <!-- גרעיניות הציון. הפרופורציה נלקחת על *כל* המקרים, ולכן כל מקרה שווה
                     allocation/count. בשני מקרים כל אחד שווה חצי מנקודות הבדיקות — וזו בדיוק
                     הקשיחות שחלוקת ליבה/קצה נועדה להסיר. אזהרה רכה, לא חסימה. -->
                <p-message
                  *ngIf="showTestGranularityWarning"
                  severity="warn"
                  styleClass="w-full mt-2"
                  [text]="testGranularityWarning"
                ></p-message>

                <!-- ⚠️ הצורה הנפוצה בקורס הזה, לא היוצא מן הכלל: רוב התרגילים מקודדים את
                     הנתונים בתוך הקוד, ואז אין מה לגוון — מקרה בדיקה אחד, ניקוד בינארי. -->
                <p-message
                  *ngIf="tests.length === 1"
                  severity="info"
                  styleClass="w-full mt-2"
                  text="למקרה בדיקה יחיד הניקוד בינארי — או הכול או כלום. אפשר להעביר חלק מהנקודות לדרישות (למשל בדיקות 50 · דרישות 50), וכך תלמידה שהפלט שלה קצת שגוי אבל הקוד שלה עומד בדרישות עדיין מקבלת עליהן. אפשרות אחרת: לנסח את התרגיל כמתודה (״כתבי FindMax(int[,] matrix)״) — אז אפשר להזין כמה קלטים שונים ולהחזיר גרעיניות לציון."
                ></p-message>

                <!-- ⚠️ אזהרה רכה בלבד. הכפייה כאן הייתה שגויה: אימות דורש פתרון לדוגמה,
                     והמורה חייבת להיות מסוגלת לשמור טיוטה בלי לכתוב אותו. -->
                <p-message
                  *ngIf="showUnverifiedWarning"
                  severity="warn"
                  styleClass="w-full mt-2"
                  text="מקרי הבדיקה לא נבדקו מול הפתרון שלך. טעות בפלט צפוי אחד מפילה את כל הכיתה על קוד תקין — מומלץ ללחוץ על ״בדיקת מקרי הבדיקה״ לפני השמירה."
                ></p-message>

                <app-test-verification-panel
                  [result]="verifyResult"
                  (fixRequested)="applyFix($event)"
                ></app-test-verification-panel>

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

                          <div class="sg-test-flag">
                            <p-checkbox
                              [inputId]="'isCore' + i"
                              formControlName="isCore"
                              [binary]="true"
                            ></p-checkbox>
                            <label [for]="'isCore' + i">
                              מקרה ליבה — בודק את עיקר התרגיל
                            </label>
                          </div>
                        </div>
                        <small class="sg-hint block mt-1">
                          מקרה שאינו דוגמה לא נשלח לתלמידה כלל — לא לפני ההגשה ולא אחרי
                          הבדיקה. היא רואה רק אם הוא עבר או נכשל.
                        </small>
                        <!-- ⚠️ ליבה הוא שער, לא משקל: מקרה ליבה שנכשל מאפס את *כל* נקודות
                             הבדיקות. הניסוח כאן חייב לומר את זה, אחרת המורה מסמנת ליבה
                             כברירת מחדל בלי לדעת שהיא בונה שער. -->
                        <small class="sg-hint block mt-1">
                          מקרה ליבה הוא שער: אם אחד מהם נכשל, נקודות הבדיקות מתאפסות לגמרי —
                          פתרון שמקודד תשובה קבועה ומצליח במקרה במקרה קצה אחד לא יזכה בנקודות.
                          מקרי קצה נספרים באופן יחסי. הורידי את הסימון ממקרים שהם באמת קצה
                          (n=0, מספר שלילי) והשאירי אותו על עיקר התרגיל.
                        </small>
                      </div>
                    </div>
                  </div>

                  <!-- ⚠️ "או", לא "וגם": תרגיל מחלקות ("כתבי מחלקה Student עם בנאי ושתי
                       תכונות") אין לו קלט, אין לו פלט ואין מה להריץ בו — הוא מנוקד על
                       המבנה בלבד וזו הגדרה לגיטימית. רק תרגיל שאין לו לא זה ולא זה נחסם. -->
                  <div
                    *ngIf="tests.length === 0"
                    class="p-3 border-1 border-round-xl"
                    [class.p-error]="!hasStructuralRules"
                    [class.text-color-secondary]="hasStructuralRules"
                    style="border-color: var(--app-border);"
                  >
                    <ng-container *ngIf="hasStructuralRules">
                      ללא מקרי בדיקה — התרגיל ינוקד על המבנה בלבד, לפי הדרישות שהגדרת.
                      זו ההגדרה הנכונה לתרגיל מחלקות, שאין לו קלט ואין לו פלט להריץ.
                    </ng-container>
                    <ng-container *ngIf="!hasStructuralRules">
                      חייב להיות לפחות מקרה בדיקה אחד <b>או</b> לפחות דרישה מבנית אחת —
                      אחרת אי אפשר לנקד את התרגיל וכל התלמידות יקבלו 0.
                    </ng-container>
                  </div>
                </div>
              </div>

              <!-- ══ דרישות התרגיל ══════════════════════════════════════════════
                   ⚠️ פתוח ובאותו משקל ויזואלי כמו מקרי הבדיקה, בכוונה: בקורס הזה רוב
                   התרגילים מקודדים את הנתונים בקוד ולכן יש להם מקרה בדיקה יחיד — הדרישות
                   הן שנושאות את רוב הנקודות ברוב התרגילים, ולא תוספת "מתקדמת". -->
              <div class="field col-12">
                <div
                  class="flex align-items-center justify-content-between gap-2 flex-wrap"
                >
                  <div class="font-bold">דרישות התרגיל</div>
                  <p-button
                    label="הוספת דרישה"
                    icon="pi pi-plus"
                    [text]="true"
                    (onClick)="addRule()"
                    type="button"
                  ></p-button>
                </div>

                <p-message
                  severity="info"
                  styleClass="w-full mt-2"
                  text="הדרישות נבדקות על הקוד עצמו (Roslyn), לא על ידי ה-AI — אותו קוד מקבל תמיד אותו ציון. כאן מנסחים את מה שכתוב במטלה: ״פתרי ברקורסיה״, ״השתמשי במשתנה בוליאני״, ״לכל היותר 3 if״."
                ></p-message>

                <div
                  formArrayName="structuralRules"
                  class="mt-3 flex flex-column gap-3"
                >
                  <div
                    *ngFor="let rule of structuralRules.controls; let i = index"
                    [formGroupName]="i"
                    class="p-3 border-1 border-round-xl"
                    style="border-color: var(--app-border); background: rgba(239,232,221,0.40)"
                  >
                    <div
                      class="flex align-items-center justify-content-between gap-2 flex-wrap mb-3"
                    >
                      <div class="font-bold text-color">
                        {{ ruleSummary(i) }}
                      </div>
                      <p-button
                        icon="pi pi-trash"
                        severity="danger"
                        [text]="true"
                        [attr.aria-label]="'מחיקת דרישה ' + (i + 1)"
                        (onClick)="removeRule(i)"
                        type="button"
                      ></p-button>
                    </div>

                    <div class="grid">
                      <div class="col-12 md:col-4">
                        <label class="block font-bold mb-2">סוג הדרישה</label>
                        <p-dropdown
                          [options]="ruleKindOptions"
                          formControlName="kind"
                          optionLabel="label"
                          optionValue="value"
                          styleClass="w-full"
                          [autoDisplayFirst]="false"
                        ></p-dropdown>
                      </div>

                      <div class="col-6 md:col-2" *ngIf="needsThreshold(i)">
                        <label class="block font-bold mb-2">כמה</label>
                        <p-inputNumber
                          formControlName="threshold"
                          [min]="1"
                          [showButtons]="true"
                          styleClass="w-full"
                        ></p-inputNumber>
                      </div>

                      <div
                        class="col-12"
                        [ngClass]="
                          needsThreshold(i) ? 'md:col-4' : 'md:col-6'
                        "
                      >
                        <label class="block font-bold mb-2">מבנה בקוד</label>
                        <p-dropdown
                          [options]="constructOptions"
                          [group]="true"
                          [filter]="true"
                          filterBy="label"
                          filterPlaceholder="חיפוש מבנה"
                          formControlName="construct"
                          optionLabel="label"
                          optionValue="value"
                          styleClass="w-full"
                          [autoDisplayFirst]="false"
                        ></p-dropdown>
                      </div>

                      <div class="col-12 md:col-8">
                        <label class="block font-bold mb-2">חומרה</label>
                        <p-selectButton
                          [options]="severityOptions"
                          formControlName="severity"
                          optionLabel="label"
                          optionValue="value"
                        ></p-selectButton>
                      </div>

                      <div class="col-6 md:col-4" *ngIf="isScored(i)">
                        <label class="block font-bold mb-2">נקודות *</label>
                        <p-inputNumber
                          formControlName="points"
                          [min]="0"
                          [max]="maxScore"
                          [showButtons]="true"
                          styleClass="w-full"
                        ></p-inputNumber>
                        <small class="p-error block mt-1" *ngIf="isScored(i) && rulePoints(i) < 1">
                          דרישה מנוקדת חייבת לשאת לפחות נקודה אחת.
                        </small>
                      </div>

                      <div class="col-12">
                        <small class="sg-hint block">{{
                          severityHint(i)
                        }}</small>
                      </div>

                      <!-- 🔴 הפרצה: while (false) { } מקיים את "חובה while" והפתרון האמיתי
                           נכתב ב-for. Roslyn סופר את הצומת ואינו יודע אם הקוד רץ. -->
                      <div class="col-12" *ngIf="missingCounterpart(i) as pair">
                        <div class="sg-rule-warning">
                          <i class="pi pi-exclamation-triangle" aria-hidden="true"></i>
                          <span>
                            דרישת "חובה" לבדה אינה סוגרת את הפרצה: אפשר לקיים אותה בשורה
                            אחת שלא רצה כלל ולפתור את התרגיל בדרך אחרת (למשל
                            {{ constructLabel(pair) }}). הבדיקה תחבירית ואינה יודעת אם
                            הקוד הגיע לשם. מומלץ להוסיף גם דרישה שאוסרת את החלופה.
                          </span>
                          <p-button
                            label="הוספת דרישה אוסרת"
                            icon="pi pi-plus"
                            [text]="true"
                            (onClick)="addCounterpart(i)"
                            type="button"
                          ></p-button>
                        </div>
                      </div>
                    </div>
                  </div>

                  <div
                    *ngIf="structuralRules.length === 0"
                    class="text-color-secondary p-3 border-1 border-round-xl"
                    style="border-color: var(--app-border);"
                  >
                    ללא דרישות — התרגיל ינוקד על מקרי הבדיקה בלבד, בדיוק כמו עד היום.
                  </div>
                </div>
              </div>

              <!-- ══ ניקוד ═══════════════════════════════════════════════════════
                   מוצג רק כשיש מה לחלק. בלי דרישות מנוקדות הבדיקות מקבלות את כל התקרה
                   אוטומטית, ואין למורה החלטה לקבל כאן. -->
              <div class="field col-12" *ngIf="showRubric">
                <div class="font-bold mb-2">ניקוד</div>

                <div class="sg-rubric">
                  <!-- ⚠️ בלי מקרי בדיקה אין שדה לערוך: נקודות בדיקות בתרגיל שאין בו מה
                       להריץ הן נקודות שאיש אינו יכול לזכות בהן, והשרת מתעלם מהן ממילא. -->
                  <div class="sg-rubric__row">
                    <label class="sg-rubric__label" for="testsAllocation"
                      >בדיקות</label
                    >
                    <ng-container *ngIf="tests.length > 0; else noTestsPoints">
                      <p-inputNumber
                        inputId="testsAllocation"
                        formControlName="testsAllocation"
                        [min]="0"
                        [max]="maxScore"
                        [showButtons]="true"
                        styleClass="sg-rubric__input"
                      ></p-inputNumber>
                      <span class="sg-hint" *ngIf="!testsAllocationManual"
                        >מחושב אוטומטית</span
                      >
                      <p-button
                        *ngIf="testsAllocationManual"
                        label="חישוב אוטומטי"
                        icon="pi pi-refresh"
                        [text]="true"
                        (onClick)="resetTestsAllocation()"
                        type="button"
                      ></p-button>
                    </ng-container>
                    <ng-template #noTestsPoints>
                      <span class="sg-rubric__value">0</span>
                      <span class="sg-hint">אין מקרי בדיקה בתרגיל</span>
                    </ng-template>
                  </div>

                  <div class="sg-rubric__row">
                    <span class="sg-rubric__label">דרישות</span>
                    <span class="sg-rubric__value">{{ rulesAllocation }}</span>
                    <span class="sg-hint">סכום הדרישות המנוקדות</span>
                  </div>

                  <div
                    class="sg-rubric__total"
                    [class.sg-rubric__total--bad]="!rubricValid"
                  >
                    <span>סה"כ</span>
                    <span>{{ rubricTotal }}</span>
                    <span>מתוך {{ maxScore }}</span>
                    <i
                      class="pi"
                      [class.pi-check]="rubricValid"
                      [class.pi-times]="!rubricValid"
                      aria-hidden="true"
                    ></i>
                  </div>
                </div>

                <!-- ⚠️ "מתוך התקרה" ולא "מתוך 100": בתרגיל בונוס התקרה גבוהה יותר,
                     ותרגיל בונוס עם "מתוך 100" נראה שבור. -->
                <small class="p-error block mt-2" *ngIf="!rubricValid">
                  הניקוד חייב להסתכם ב-{{ maxScore }} בדיוק: בדיקות + סכום הנקודות של
                  הדרישות המנוקדות.
                </small>

                <small class="sg-hint block mt-2" *ngIf="rubricSplitHint">{{
                  rubricSplitHint
                }}</small>
              </div>

              <!-- ⚠️ מוצג רק במקרה "הכול שערים": אין טסטים ואין דרישות מנוקדות, רק חוסמות.
                   זו הצורה הטבעית של תרגיל מחלקות, ואין בה מה לחלק. -->
              <div class="field col-12" *ngIf="isAllGates">
                <p-message
                  severity="info"
                  styleClass="w-full"
                  text="בתרגיל הזה אין מקרי בדיקה ואין דרישות מנוקדות — רק דרישות חוסמות. תלמידה שעומדת בכולן מקבלת 100, ומי שלא עומדת באחת מהן אינה מקבלת ציון ומגישה שוב."
                ></p-message>
              </div>

              <div class="field col-12 md:col-6">
                <label class="block font-bold mb-2" for="retryThreshold"
                  >סף להגשה חוזרת</label
                >
                <p-inputNumber
                  inputId="retryThreshold"
                  formControlName="retryThreshold"
                  [min]="0"
                  [max]="100"
                  [showButtons]="true"
                  styleClass="w-full"
                ></p-inputNumber>
                <small class="sg-hint block mt-1">
                  מתחת לציון הזה ההגשה נשארת פתוחה והתלמידה רשאית לתקן ולהגיש שוב, בלי
                  הגבלת ניסיונות. מהציון הזה ומעלה ההגשה ננעלת, ורק את יכולה לפתוח אותה.
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

    <p-confirmDialog></p-confirmDialog>

    <app-test-suggestions-dialog
      [(visible)]="suggestDialogVisible"
      [loading]="suggesting"
      [result]="suggestResult"
      (addSelected)="addSuggestedTests($event)"
    ></app-test-suggestions-dialog>
  `,
  styles: [
    `
      .sg-ref-filename {
        flex: 1;
        min-width: 12rem;
      }

      /* קוד הוא תמיד LTR גם בטופס עברי — הזחות ותווי סוגריים נשברים לגמרי ב-RTL. */
      .sg-code-input {
        direction: ltr;
        text-align: left;
        font-family: "Consolas", "Courier New", monospace;
        font-size: var(--text-sm);
        line-height: 1.5;
      }

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

      /* שורת אזהרת הצמד — צהוב סמנטי, אותם טוקנים כמו שאר המערכת */
      .sg-rule-warning {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: var(--space-2);
        padding: var(--space-2) var(--space-3);
        border-radius: var(--radius-md);
        background: var(--status-warn-bg);
        color: var(--status-warn-ink);
        font-size: var(--text-sm);
        line-height: 1.5;
      }

      .sg-rule-warning span {
        flex: 1;
        min-width: 14rem;
      }

      .sg-rubric {
        display: flex;
        flex-direction: column;
        gap: var(--space-2);
        padding: var(--space-3);
        border: 1px solid var(--app-border);
        border-radius: var(--radius-md);
        max-width: 34rem;
      }

      .sg-rubric__row {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: var(--space-3);
      }

      .sg-rubric__label {
        font-weight: 600;
        min-width: 5rem;
        margin: 0;
      }

      .sg-rubric__value {
        font-weight: 700;
        min-width: 3rem;
      }

      .sg-rubric__input {
        width: 8rem;
      }

      .sg-rubric__total {
        display: flex;
        align-items: center;
        gap: var(--space-2);
        padding-top: var(--space-2);
        border-top: 1px solid var(--app-border);
        font-weight: 800;
        color: var(--status-success-ink);
      }

      .sg-rubric__total--bad {
        color: var(--status-error-ink);
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

  /** כמה מקרים מבקשים מה-AI. תואם ל-SuggestTestCasesLimits בשרת, שאוכף את התקרה. */
  private static readonly SuggestCount = 5;

  /** מצב פתיחה התחלתי של קטע הפתרון לדוגמה. נקבע פעם אחת — ר' ההערה בתבנית. */
  referencePanelCollapsed = true;

  verifying = false;
  verifyResult: VerifyTestCasesResultDto | null = null;

  /**
   * מדכא את איפוס תוצאת האימות בזמן שהקוד עצמו כותב לטופס.
   * ⚠️ בלי זה לחיצה על "תיקון" הייתה מוחקת את כל הפאנל: המורה מתקנת שורה אחת ומאבדת
   * את שאר השורות שנכשלו, ונאלצת להריץ מחדש אחרי כל תיקון.
   */
  private applyingFix = false;

  suggesting = false;
  suggestDialogVisible = false;
  suggestResult: SuggestTestCasesResultDto | null = null;

  readonly gradingModeOptions: { label: string; value: GradingMode }[] =
    Object.entries(GRADING_MODE_LABELS_HE).map(([value, label]) => ({
      label,
      value: value as GradingMode,
    }));

  // ⚠️ מי מדפיס — זו ההבחנה שקובעת את הבחירה, והיא הייתה חסרה כאן לגמרי. במצב "מתודה בודדת"
  // העטיפה מדפיסה את הערך המוחזר; אם התלמידה מדפיסה גם היא, ב-stdout יש שתי שורות והבדיקה
  // נכשלת למרות שהלוגיקה נכונה. לתרגיל מסוג "קלטי מספר והדפיסי את הסכום" המצב הנכון הוא
  // "תוכנית שלמה" — שם התלמידה היא זו שמדפיסה.
  readonly gradingModeDescriptions: Record<GradingMode, string> = {
    FullProgram:
      'תוכנית שלמה כולל using/class/Main. התלמידה קוראת את הקלט ומדפיסה את הפלט בעצמה. זה המצב הנכון לתרגיל מסוג "קלטי מספר והדפיסי את הסכום", וגם לתחילת הקורס (לולאות, מערכים, מטריצות).',
    Method:
      'רק גוף מתודה בודדת, בלי class/using/Main. המערכת קוראת למתודה ומדפיסה את הערך המוחזר — התלמידה לא מדפיסה בעצמה. זה המצב הנכון לתרגיל מסוג "כתבי מתודה שמחזירה את הסכום".',
    MultiFileMethod:
      "כמה קבצים (למשל מחלקות), ונבדקת קריאה למתודת כניסה מוגדרת — בלי Main של התלמידה. גם כאן המערכת מדפיסה את הערך המוחזר.",
  };

  constructor() {
    this.form = this.fb.group(
      {
        title: ["", Validators.required],
        description: [""],
        methodName: [""],
        gradingMode: ["FullProgram" as GradingMode, Validators.required],
        isBonus: [false],
        bonusValue: [0],
        // ⚠️ ללא Validators.required: תרגיל מחלקות מנוקד על המבנה בלבד ואין לו מה
        // להריץ. הכלל האמיתי הוא "טסט אחד *או* דרישה אחת", והוא נאכף ברמת הטופס
        // ב-gradeabilityValidator — בדיוק כמו AssignmentGradeability בשרת.
        tests: this.fb.array([]),
        structuralRules: this.fb.array([]),
        expectedFiles: this.fb.array([]),
        referenceSolution: this.fb.array([]),
        testsAllocation: [TOTAL_POINTS],
        retryThreshold: [
          DEFAULT_RETRY_THRESHOLD,
          [Validators.min(0), Validators.max(TOTAL_POINTS)],
        ],
      },
      { validators: [gradeabilityValidator] },
    );

    // ⚠️ הסדר חשוב: המנוי הזה נרשם לפני כל setValue אוטומטי, וכל השמה אוטומטית
    // נעשית עם emitEvent:false. לכן כל אירוע שמגיע לכאן הוא בהכרח הקלדה של המורה —
    // וזה מה שמסמן "אל תדרוס לה את המספר יותר".
    this.form
      .get("testsAllocation")
      ?.valueChanges.subscribe(() => (this.testsAllocationManual = true));

    // הרובריקה מסתכמת בתקרה. כל אחד מהשלושה משנה אותה: נקודות הדרישות, מספר מקרי
    // הבדיקה (0 טסטים = 0 נקודות בדיקות), והתקרה עצמה בתרגיל בונוס.
    this.form
      .get("structuralRules")
      ?.valueChanges.subscribe(() => this.recomputeTestsAllocation());
    this.form
      .get("tests")
      ?.valueChanges.subscribe(() => this.recomputeTestsAllocation());
    this.form
      .get("isBonus")
      ?.valueChanges.subscribe(() => this.recomputeTestsAllocation());
    this.form
      .get("bonusValue")
      ?.valueChanges.subscribe(() => this.recomputeTestsAllocation());

    // MethodName נדרש רק במצב "מתודה בודדת" — שאר המצבים לא תלויים בו.
    this.form.get("gradingMode")?.valueChanges.subscribe(() => {
      this.updateMethodNameValidator();
    });
    this.updateMethodNameValidator();

    // ⚠️ תוצאת אימות מתיישנת ברגע שמשהו שהשפיע עליה משתנה. בלי זה המורה מתקנת פלט
    // צפוי, נשארת מולה שורת ✅ ירוקה מההרצה הקודמת, ושומרת בביטחון מלא על סמך תוצאה
    // שכבר לא מתארת את מה שבטופס.
    this.form.get("tests")?.valueChanges.subscribe(() => this.invalidateVerification());
    this.form.get("referenceSolution")?.valueChanges.subscribe(() => this.invalidateVerification());
    this.form.get("gradingMode")?.valueChanges.subscribe(() => this.invalidateVerification());
    this.form.get("methodName")?.valueChanges.subscribe(() => this.invalidateVerification());
    this.form.get("expectedFiles")?.valueChanges.subscribe(() => this.invalidateVerification());
  }

  private invalidateVerification(): void {
    if (this.applyingFix) return;
    this.verifyResult = null;
  }

  get tests(): FormArray {
    return this.form.get("tests") as FormArray;
  }

  get expectedFiles(): FormArray {
    return this.form.get("expectedFiles") as FormArray;
  }

  get referenceSolution(): FormArray {
    return this.form.get("referenceSolution") as FormArray;
  }

  // ── דרישות מבניות ────────────────────────────────────────────────────────

  readonly ruleKindOptions: { label: string; value: RuleKind }[] = (
    Object.keys(RULE_KIND_LABELS_HE) as RuleKind[]
  ).map((value) => ({ label: RULE_KIND_LABELS_HE[value], value }));

  readonly severityOptions: { label: string; value: RuleSeverity }[] = (
    Object.keys(RULE_SEVERITY_LABELS_HE) as RuleSeverity[]
  ).map((value) => ({ label: RULE_SEVERITY_LABELS_HE[value], value }));

  /** מקובץ לפי סדר ההוראה בכיתה — p-dropdown עם [group]="true". */
  readonly constructOptions = CODE_CONSTRUCT_GROUPS.map((group) => ({
    label: group.label,
    items: group.items.map((value) => ({
      label: CODE_CONSTRUCT_LABELS_HE[value],
      value,
    })),
  }));

  /**
   * המורה הקלידה בעצמה את נקודות הבדיקות.
   * ⚠️ מרגע זה החישוב האוטומטי מפסיק. מספר שהמורה קבעה בכוונה (למשל 50 בתרגיל עם מקרה
   * בדיקה יחיד) לא יזוז מתחת לידיים שלה כשהיא מוסיפה עוד מקרה.
   */
  testsAllocationManual = false;

  get structuralRules(): FormArray {
    return this.form.get("structuralRules") as FormArray;
  }

  get rulesValue(): StructuralRuleDto[] {
    return this.structuralRules.value as StructuralRuleDto[];
  }

  get hasStructuralRules(): boolean {
    return this.structuralRules.length > 0;
  }

  get hasScoredRule(): boolean {
    return this.rulesValue.some((r) => r.severity === "Scored");
  }

  /** התקרה: 100, או 100 + הבונוס. נגזרת ואינה עמודה נפרדת — ר' Assignment.MaxScore. */
  get maxScore(): number {
    return maxScoreOf(
      !!this.form.get("isBonus")?.value,
      Number(this.form.get("bonusValue")?.value ?? 0),
    );
  }

  get rulesAllocation(): number {
    return scoredRulePoints(this.rulesValue);
  }

  get testsAllocationValue(): number {
    return Number(this.form.get("testsAllocation")?.value ?? 0);
  }

  /** מה שנספר בפועל: תרגיל בלי מקרי בדיקה אינו מקבל נקודות בדיקות גם אם הוקצו לו. */
  get rubricTotal(): number {
    return (
      (this.tests.length > 0 ? this.testsAllocationValue : 0) +
      this.rulesAllocation
    );
  }

  get rubricValid(): boolean {
    return hasValidRubric(
      this.maxScore,
      this.testsAllocationValue,
      this.tests.length,
      this.rulesValue,
    );
  }

  /**
   * בלי דרישות מנוקדות אין למורה החלטה לקבל — הבדיקות מקבלות את כל התקרה אוטומטית.
   * החריג: רובריקה שאינה תקינה חייבת להיות גלויה, אחרת אי אפשר לתקן אותה.
   */
  get showRubric(): boolean {
    return this.hasScoredRule || !this.rubricValid;
  }

  /** אין טסטים ואין דרישות מנוקדות — הצורה הטבעית של תרגיל מחלקות. */
  get isAllGates(): boolean {
    return (
      this.tests.length === 0 &&
      this.hasStructuralRules &&
      this.rulesAllocation === 0
    );
  }

  /**
   * ההמלצה לפיצול — טקסט בלבד.
   * ⚠️ בכוונה אינה מזיזה מספרים: פיצול שמשתנה לבד כשמוסיפים מקרה בדיקה דורס בחירה
   * מודעת של המורה, ובדיוק בתרגילים שבהם היא בחרה בכוונה.
   */
  get rubricSplitHint(): string | null {
    if (!this.hasScoredRule) return null;

    if (this.tests.length === 0)
      return "אין מקרי בדיקה — כל הנקודות על הדרישות.";
    if (this.tests.length === 1)
      return `מומלץ בתרגיל עם מקרה בדיקה יחיד: בדיקות ${Math.round(
        this.maxScore / 2,
      )} · דרישות ${this.maxScore - Math.round(this.maxScore / 2)}.`;
    return "מומלץ בתרגיל עם כמה מקרי בדיקה: בדיקות 80 · דרישות 20.";
  }

  /** 2–3 מקרים בלבד. מקרה יחיד מקבל הודעה מפורטת משלו. */
  get showTestGranularityWarning(): boolean {
    return (
      this.tests.length >= 2 &&
      this.tests.length < 4 &&
      this.testsAllocationValue > 0
    );
  }

  get testGranularityWarning(): string {
    const each =
      Math.round((this.testsAllocationValue / this.tests.length) * 10) / 10;
    return `עם ${this.tests.length} מקרי בדיקה כל אחד שווה ${each} נקודות — מקרה קצה אחד ששכחת עולה הרבה. מומלץ 4-6 מקרים.`;
  }

  constructLabel(construct: CodeConstruct): string {
    return CODE_CONSTRUCT_LABELS_HE[construct] ?? construct;
  }

  /** תיאור הדרישה בכותרת השורה — בדיוק הניסוח שהתלמידה תראה בתוצאה. */
  ruleSummary(index: number): string {
    const rule = this.structuralRules.at(index)?.value as StructuralRuleDto;
    if (!rule?.construct) return `דרישה ${index + 1}`;
    return `${describeRule(rule)} · ${RULE_SEVERITY_LABELS_HE[rule.severity]}`;
  }

  needsThreshold(index: number): boolean {
    const kind = this.structuralRules.at(index)?.get("kind")?.value as RuleKind;
    return kind === "AtLeast" || kind === "AtMost";
  }

  isScored(index: number): boolean {
    return this.structuralRules.at(index)?.get("severity")?.value === "Scored";
  }

  rulePoints(index: number): number {
    return Number(this.structuralRules.at(index)?.get("points")?.value ?? 0);
  }

  severityHint(index: number): string {
    const severity = this.structuralRules.at(index)?.get("severity")
      ?.value as RuleSeverity;
    return RULE_SEVERITY_HINTS_HE[severity] ?? "";
  }

  /**
   * דרישת "חובה" שאין לה דרישה אוסרת מקבילה.
   * 🔴 בלי הצמד אפשר לקיים את הדרישה בשורה מתה ולפתור את התרגיל בדרך אחרת. Roslyn סופר
   * צמתים ואינו יודע אם הקוד בכלל רץ; זיהוי הישֵגוּת מחוץ להיקף, והצמד הוא הבקרה המעשית.
   */
  missingCounterpart(index: number): CodeConstruct | null {
    const rule = this.structuralRules.at(index)?.value as StructuralRuleDto;
    if (!rule || rule.kind !== "MustUse") return null;

    const pair = PAIRED_CONSTRUCT[rule.construct];
    if (!pair) return null;

    const covered = this.rulesValue.some(
      (r) => r.kind === "MustNotUse" && r.construct === pair,
    );
    return covered ? null : pair;
  }

  createRuleGroup(rule?: StructuralRuleDto): FormGroup {
    const group = this.fb.group({
      kind: [rule?.kind ?? ("MustUse" as RuleKind), Validators.required],
      construct: [
        rule?.construct ?? ("Recursion" as CodeConstruct),
        Validators.required,
      ],
      threshold: [rule?.threshold ?? 1],
      // חוסמת כברירת מחדל: זו הניסוח שהמורה מתחילה ממנו ("אם דרשתי רקורסיה והיא כתבה
      // לולאות — כאילו לא עשתה"), והיא גם היחידה שאינה נוגעת ברובריקה.
      severity: [rule?.severity ?? ("Blocking" as RuleSeverity)],
      points: [rule?.points ?? 0],
    });

    // דרישה מנוקדת בלי נקודות אינה עושה כלום, ודרישה חוסמת עם נקודות אינה קיימת —
    // השדות מתיישרים לפי החומרה במקום להשאיר למורה לנקות אותם ביד.
    group.get("severity")?.valueChanges.subscribe((severity) => {
      const points = group.get("points");
      if (!points) return;

      if (severity === "Scored" && Number(points.value ?? 0) < 1) {
        points.setValue(10);
      } else if (severity !== "Scored" && Number(points.value ?? 0) !== 0) {
        points.setValue(0);
      }
    });

    // סף 0 ב-AtLeast/AtMost נדחה בשרת, ולכן הוא מתוקן כאן ברגע שהוא הופך לרלוונטי.
    group.get("kind")?.valueChanges.subscribe((kind) => {
      const threshold = group.get("threshold");
      if (!threshold) return;

      if (
        (kind === "AtLeast" || kind === "AtMost") &&
        Number(threshold.value ?? 0) < 1
      ) {
        threshold.setValue(1);
      }
    });

    return group;
  }

  addRule(): void {
    this.structuralRules.push(this.createRuleGroup());
    this.structuralRules.markAsDirty();
  }

  /**
   * מוסיפה את הדרישה האוסרת שסוגרת את הפרצה.
   * ⚠️ תמיד חוסמת: היא שער שנועד לפסול דרך פתרון, אינה נושאת ניקוד, ולכן גם אינה
   * משנה את הרובריקה שהמורה כבר איזנה.
   */
  addCounterpart(index: number): void {
    const pair = this.missingCounterpart(index);
    if (!pair) return;

    this.structuralRules.push(
      this.createRuleGroup({
        kind: "MustNotUse",
        construct: pair,
        threshold: 0,
        severity: "Blocking",
        points: 0,
      }),
    );
    this.structuralRules.markAsDirty();
  }

  removeRule(index: number): void {
    this.confirmationService.confirm({
      message: `האם למחוק את הדרישה "${this.ruleSummary(index)}"? לא ניתן לשחזר פעולה זו.`,
      header: "אישור מחיקה",
      icon: "pi pi-exclamation-triangle",
      acceptLabel: "מחיקה",
      rejectLabel: "ביטול",
      accept: () => this.structuralRules.removeAt(index),
    });
  }

  // ── הרובריקה ─────────────────────────────────────────────────────────────

  private recomputeTestsAllocation(): void {
    if (this.testsAllocationManual) return;

    const control = this.form.get("testsAllocation");
    if (!control) return;

    // תרגיל בלי מקרי בדיקה אינו מקבל נקודות בדיקות — הן היו נקודות שאיש אינו יכול לזכות בהן.
    const next =
      this.tests.length === 0
        ? 0
        : Math.max(0, Math.min(this.maxScore, this.maxScore - this.rulesAllocation));

    if (Number(control.value ?? 0) === next) return;

    // emitEvent:false שומר על המשמעות של testsAllocationManual — ר' ההערה בבנאי.
    control.setValue(next, { emitEvent: false });
    this.form.updateValueAndValidity({ emitEvent: false });
  }

  /** מחזירה את השדה לחישוב אוטומטי אחרי שהמורה ערכה אותו ידנית. */
  resetTestsAllocation(): void {
    this.testsAllocationManual = false;
    this.recomputeTestsAllocation();
  }


  /** האם יש פתרון לדוגמה עם תוכן ממשי — שורה ריקה אינה פתרון. */
  get hasReferenceSolution(): boolean {
    return this.referenceSolution.controls.some(
      (c) => !!(c.get("content")?.value as string | null)?.trim(),
    );
  }

  get canVerify(): boolean {
    return this.hasReferenceSolution && this.tests.length > 0 && !this.verifying;
  }

  get verifyTooltip(): string {
    if (!this.hasReferenceSolution)
      return "כדי לבדוק את מקרי הבדיקה צריך להזין פתרון לדוגמה";
    if (this.tests.length === 0) return "אין מקרי בדיקה לבדוק";
    return "מריץ את הפתרון שלך מול כל מקרי הבדיקה. שום דבר לא נשמר.";
  }

  /**
   * הצעה עובדת גם בלי פתרון לדוגמה — אז ההצעות חוזרות מסומנות "לא אומת". תיאור התרגיל
   * לעומת זאת הוא חובה: בלעדיו המודל ממציא תרגיל משלו.
   */
  get canSuggest(): boolean {
    return (
      !!(this.form.get("description")?.value as string | null)?.trim() &&
      !this.suggesting
    );
  }

  get suggestTooltip(): string {
    if (!(this.form.get("description")?.value as string | null)?.trim())
      return "צריך למלא את תיאור התרגיל כדי שה-AI יוכל להציע מקרי בדיקה";
    return this.hasReferenceSolution
      ? "ההצעות ייבדקו מול הפתרון שלך לפני שיוצגו"
      : "ללא פתרון לדוגמה ההצעות לא ייבדקו — הן יסומנו כלא מאומתות";
  }

  /**
   * אזהרה רכה לפני שמירה. מוצגת רק כשיש פתרון לדוגמה — בלעדיו אין מה לבדוק מולו,
   * ואזהרה שאי אפשר לפעול לפיה היא רעש.
   */
  get showUnverifiedWarning(): boolean {
    return (
      this.tests.length > 0 && this.hasReferenceSolution && this.verifyResult === null
    );
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

  /**
   * מה בדיוק להדביק בשדה הפתרון לדוגמה, לפי מצב ההרצה.
   *
   * ⚠️ במצב Method העטיפה של ה-Runner מדביקה את הקוד לתוך `static class StudentSolution`,
   * ולכן מחלקה עוטפת משלך הופכת למחלקה מקוננת והקומפילציה נופלת על
   * `CS0117: StudentSolution does not contain a definition for ...`. השגיאה מצביעה על
   * מחלקה שהמורה לא כתבה ולא ראתה מעולם, ולכן ההבדל הזה נאמר כאן מראש ולא מתגלה בהרצה.
   */
  get referenceSolutionHint(): string {
    switch (this.gradingMode) {
      case "FullProgram":
        return "במצב הזה הדביקי תוכנית שלמה, כולל Main — בדיוק כמו שהתלמידה תגיש.";
      case "MultiFileMethod":
        return "במצב הזה הדביקי את המחלקות עצמן (אפשר כמה קבצים), בלי Main. מתודת הכניסה חייבת להיות static.";
      default:
        return "במצב הזה הדביקי את המתודה בלבד, בלי מחלקה עוטפת ובלי Main — המערכת עוטפת אותה בעצמה. המתודה חייבת להיות static.";
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
          retryThreshold: assignment.retryThreshold ?? DEFAULT_RETRY_THRESHOLD,
        });

        // ⚠️ הרובריקה השמורה היא החלטה של המורה, לא הצעה: היא נטענת כמות שהיא ומסומנת
        // כידנית, אחרת פתיחת התרגיל לעריכה הייתה משכתבת בשקט חלוקה שנקבעה בכוונה.
        this.form
          .get("testsAllocation")
          ?.setValue(assignment.testsAllocation ?? TOTAL_POINTS, {
            emitEvent: false,
          });
        this.testsAllocationManual = true;

        if (assignment.structuralRules) {
          assignment.structuralRules.forEach((rule) => {
            this.structuralRules.push(this.createRuleGroup(rule));
          });
        }

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

        // ⚠️ בתצוגת תלמידה השרת מרוקן את השדה הזה, ולכן הרשימה תהיה ריקה. המסך הזה
        // חסום ממילא ל-Teacher/Admin, וההסתרה האמיתית היא זו שבשרת.
        if (assignment.referenceSolution) {
          assignment.referenceSolution.forEach((file) => {
            this.referenceSolution.push(this.createReferenceFileGroup(file));
          });
        }

        // פתוח בעריכה כשכבר יש פתרון, סגור אחרת — נקבע כאן פעם אחת ולא נגזר מהטופס.
        this.referencePanelCollapsed = !this.hasReferenceSolution;

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
      // ברירת המחדל true, בניגוד ל-isSample: רוב המקרים הם מקרי ליבה והמורה מורידה
      // את הסימון מהמיעוט. ר' TestCase.IsCore בשרת.
      isCore: [testCase?.isCore ?? true],
    });
  }

  /** האם קיים לפחות מקרה דוגמה אחד — בסיס לאזהרה הרכה בטופס. */
  get hasSampleTest(): boolean {
    return this.tests.controls.some((c) => c.get("isSample")?.value === true);
  }

  addTestCase(): void {
    // המקרה הראשון מסומן כדוגמה כברירת מחדל: תרגיל בלי אף דוגמה משאיר את התלמידה בלי
    // שום רמז לפורמט הקלט. כל מקרה נוסף מוסתר כברירת מחדל (fail closed).
    this.tests.push(
      this.createTestCaseGroup({
        input: "",
        expected: "",
        isSample: this.tests.length === 0,
        isCore: true,
      }),
    );
  }

  removeTestCase(index: number): void {
    // מחיקת מקרה מלא היא איבוד עבודה אמיתי — עד עכשיו שורה נמחקה בלחיצה אחת בלי אזהרה,
    // ו-PUT עם רשימה ריקה מחק בשקט את כל מקרי הבדיקה של התרגיל.
    const group = this.tests.at(index);
    const hasContent =
      !!group?.get("input")?.value || !!group?.get("expected")?.value;

    if (!hasContent) {
      this.tests.removeAt(index);
      return;
    }

    this.confirmationService.confirm({
      message: `האם למחוק את מקרה בדיקה ${index + 1}? לא ניתן לשחזר פעולה זו.`,
      header: "אישור מחיקה",
      icon: "pi pi-exclamation-triangle",
      acceptLabel: "מחיקה",
      rejectLabel: "ביטול",
      accept: () => this.tests.removeAt(index),
    });
  }

  // ── פתרון לדוגמה ────────────────────────────────────────────────────────

  createReferenceFileGroup(file?: ReferenceSolutionFileDto): FormGroup {
    return this.fb.group({
      fileName: [file?.fileName || ""],
      content: [file?.content || ""],
    });
  }

  addReferenceFile(): void {
    // שם ברירת מחדל ולא שדה ריק: ברוב התרגילים יש קובץ אחד, והמורה לא אמורה להתעסק
    // בשמות קבצים כדי לבדוק את עצמה.
    this.referenceSolution.push(
      this.createReferenceFileGroup({
        fileName: this.referenceSolution.length === 0 ? "Solution.cs" : "",
        content: "",
      }),
    );
  }

  removeReferenceFile(index: number): void {
    const hasContent = !!(
      this.referenceSolution.at(index)?.get("content")?.value as string | null
    )?.trim();

    if (!hasContent) {
      this.referenceSolution.removeAt(index);
      return;
    }

    this.confirmationService.confirm({
      message: "האם למחוק את הפתרון לדוגמה? לא ניתן לשחזר פעולה זו.",
      header: "אישור מחיקה",
      icon: "pi pi-exclamation-triangle",
      acceptLabel: "מחיקה",
      rejectLabel: "ביטול",
      accept: () => this.referenceSolution.removeAt(index),
    });
  }

  // ── בדיקת מקרי הבדיקה ───────────────────────────────────────────────────

  verifyTests(): void {
    if (!this.canVerify) return;

    this.verifying = true;
    this.verifyResult = null;

    this.assignmentsService
      .verifyTests(this.lessonId, {
        gradingMode: this.gradingMode,
        methodName: this.form.get("methodName")?.value ?? null,
        referenceSolution: this.referenceSolutionValue(),
        expectedFiles: this.expectedFiles.value as ExpectedFileDto[],
        tests: this.tests.value as TestCaseDto[],
      })
      .subscribe({
        next: (result) => {
          // ⚠️ ההשמה חייבת לבוא *אחרי* ניקוי הדגל, אחרת מנוי ה-valueChanges שמאפס את
          // התוצאה עלול לרוץ ולמחוק אותה מיד. אין כאן שינוי טופס, אבל הסדר מגן על זה.
          this.verifying = false;
          this.verifyResult = result;
        },
        error: (_error: unknown) => {
          // ApiErrorInterceptor כבר מציג את הודעת ה-BusinessRuleException (למשל
          // "מערכת בדיקת הקוד אינה זמינה"), ולכן אין כאן טוסט כפול.
          this.verifying = false;
        },
      });
  }

  /**
   * כותב לשדה "פלט צפוי" את הערך שהפתרון החזיר בפועל.
   *
   * השורה מסומנת כעוברת בלי הרצה חוזרת, וזו לא קיצור דרך: הערך החדש הוא <b>בדיוק</b> מה
   * שהפתרון החזיר על הקלט הזה בהרצה שזה עתה רצה, ולכן הוא עובר בהגדרה. הרצה מחדש הייתה
   * מבזבזת סבב Judge0 שלם כדי לאשר משהו שכבר ידוע.
   */
  applyFix(index: number): void {
    const row = this.verifyResult?.results.find((r) => r.index === index);
    const control = this.tests.at(index)?.get("expected");

    if (!row || !control || !row.canFix) return;

    this.applyingFix = true;
    control.setValue(row.actual);
    control.markAsDirty();
    this.applyingFix = false;

    if (!row.passed) {
      row.passed = true;
      row.expected = row.actual;
      if (this.verifyResult) this.verifyResult.passed += 1;
    }

    this.messageService.add({
      severity: "success",
      summary: "עודכן",
      detail: `הפלט הצפוי של בדיקה ${index + 1} עודכן ל-${row.actual}`,
    });
  }

  // ── הצעות AI ────────────────────────────────────────────────────────────

  suggestTests(): void {
    if (!this.canSuggest) return;

    this.suggesting = true;
    this.suggestResult = null;
    this.suggestDialogVisible = true;

    this.assignmentsService
      .suggestTests(this.lessonId, {
        description: this.form.get("description")?.value ?? "",
        gradingMode: this.gradingMode,
        methodName: this.form.get("methodName")?.value ?? null,
        count: AssignmentFormComponent.SuggestCount,
        referenceSolution: this.referenceSolutionValue(),
        expectedFiles: this.expectedFiles.value as ExpectedFileDto[],
      })
      .subscribe({
        next: (result) => {
          this.suggesting = false;
          this.suggestResult = result;
        },
        error: (_error: unknown) => {
          // ההודעה מגיעה מה-interceptor. סוגרים את החלון כדי לא להשאיר ספינר תקוע —
          // כתיבה ידנית ובדיקה ממשיכות לעבוד בדיוק כמקודם.
          this.suggesting = false;
          this.suggestDialogVisible = false;
        },
      });
  }

  /** ההצעות נכנסות לטופס רק כאן — אחרי שהמורה סימנה ואישרה. */
  addSuggestedTests(cases: SuggestedTestCaseDto[]): void {
    for (const item of cases) {
      this.tests.push(
        this.createTestCaseGroup({
          input: item.input,
          // הערך שנשמר הוא זה שהורץ, לא זה שהמודל הציע. ר' SuggestedTestCaseDto.expected.
          expected: item.expected,
          // הצעה חדשה מוסתרת כברירת מחדל (fail closed) — אלא אם אין עדיין אף דוגמה.
          isSample: !this.hasSampleTest,
          isCore: item.isCore,
        }),
      );
    }

    this.tests.markAsDirty();

    this.messageService.add({
      severity: "success",
      summary: "נוספו",
      detail: `${cases.length} מקרי בדיקה נוספו לטופס. הם ייכנסו לתרגיל רק כשתשמרי.`,
    });
  }

  /**
   * מנרמל את הדרישות לפני השליחה: סף רק היכן שיש לו משמעות, נקודות רק לדרישה מנוקדת.
   * ⚠️ ערך שנשאר בשדה מוסתר (נקודות של דרישה שהפכה לחוסמת) חוזר להטעות בעריכה הבאה.
   */
  private structuralRulesValue(): StructuralRuleDto[] {
    return this.rulesValue.map((rule) => ({
      kind: rule.kind,
      construct: rule.construct,
      threshold:
        rule.kind === "AtLeast" || rule.kind === "AtMost"
          ? Number(rule.threshold ?? 0)
          : 0,
      severity: rule.severity,
      points: rule.severity === "Scored" ? Number(rule.points ?? 0) : 0,
    }));
  }

  /** רק קבצים עם תוכן — שורה ריקה מייצרת שגיאת קומפילציה מיותרת ב-Judge0. */
  private referenceSolutionValue(): ReferenceSolutionFileDto[] {
    return (this.referenceSolution.value as ReferenceSolutionFileDto[]).filter(
      (f) => !!f.content?.trim(),
    );
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
      referenceSolution: this.referenceSolutionValue(),
      structuralRules: this.structuralRulesValue(),
      // תרגיל בלי מקרי בדיקה שולח 0 ולא את מה שנשאר בשדה — השרת מתעלם ממנו ממילא,
      // ומספר "מת" בבסיס הנתונים חוזר להטעות בעריכה הבאה.
      testsAllocation: this.tests.length > 0 ? this.testsAllocationValue : 0,
      retryThreshold: Number(formValue.retryThreshold ?? DEFAULT_RETRY_THRESHOLD),
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
