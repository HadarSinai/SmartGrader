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
  templateUrl: "./assignment-form.component.html",
  styleUrls: ["./assignment-form.component.css"],
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

  /**
   * שני שלבים, לא טאבים ולא טופס אחד ארוך.
   * <p>
   * <b>למה לא טאבים:</b> הרובריקה חייבת להסתכם בתקרת התרגיל (‎G-14‎), והתקרה
   * נקבעת בשלב 1 — היא ‎100‎ או ‎100 + bonusValue‎ (‎G-17‎). בניווט חופשי בין
   * טאבים מורה יכולה לחלק נקודות לפני שהצהירה על הבונוס, ואז להישפט מול מספר
   * שהמסך מעולם לא הראה לה. שלבים הופכים את התלות לסדר.
   * </p>
   * <p>
   * <b>למה לא טופס אחד ארוך:</b> אי אפשר לראות בו-זמנית את סכום הרובריקה ואת
   * הנקודות שמקלידים — הסכום למטה, השורות מעליו, וכל בדיקה היא גלילה הלוך ושוב.
   * </p>
   * <p>
   * ⚠️ הרכיב <i>לא</i> פוצל לשני רכיבים: בלוק הדרישות חולק מצב ‎FormArray‎ עם
   * הגטרים של הרובריקה, והפרדה שלו היא ריפקטור אמיתי ולא העברה. השלבים הם
   * הצגה, והטופס אחד.
   * </p>
   */
  step: 1 | 2 = 1;

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

  /** שדות שלב 1 בלבד. שלב 2 אינו נפתח לפני שהתקרה ידועה. */
  private static readonly StepOneControls = [
    "title",
    "description",
    "gradingMode",
    "methodName",
    "isBonus",
    "bonusValue",
  ];

  /**
   * ⚠️ בודק רק את שדות שלב 1. ‎form.invalid‎ כולל את מקרי הבדיקה והדרישות, שהמורה
   * עוד לא הגיעה אליהם — חסימה עליהם הייתה מונעת ממנה להתקדם אל המסך שבו ממלאים
   * אותם.
   */
  get stepOneValid(): boolean {
    return AssignmentFormComponent.StepOneControls.every(
      (name) => this.form.get(name)?.valid !== false,
    );
  }

  goToScoring(): void {
    if (!this.stepOneValid) {
      // בלי הסימון השגיאות אינן מוצגות: הן תלויות ב-touched, והמורה לא נגעה בשדה
      for (const name of AssignmentFormComponent.StepOneControls) {
        this.form.get(name)?.markAsTouched();
      }
      return;
    }
    this.step = 2;
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  backToDetails(): void {
    this.step = 1;
    window.scrollTo({ top: 0, behavior: "smooth" });
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
