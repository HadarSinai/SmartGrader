import { CommonModule } from "@angular/common";
import { Component, Input } from "@angular/core";

import { PanelModule } from "primeng/panel";
import { TableModule } from "primeng/table";
import { TabViewModule } from "primeng/tabview";
import { TagModule } from "primeng/tag";

import { RULE_SEVERITY_LABELS_HE, RuleSeverity } from "@models/assignment.model";
import {
  StructuralRuleResultDto,
  SubmissionResponseDto,
  TestCaseResultDto,
} from "@models/submission.model";

// מרכז את כל תצוגת המשוב המובנה (פירוק הציון, דרישות, קטגוריות, טסטים) במקום אחד, כדי
// שהלוגיקה לא תוכפל בין מסך הסטודנט (my-feedback) למסך המורה (submission-detail).
//
// 🔴 מה השתנה כאן: ארבעת אריחי הציון שה-AI ניקד בעצמו הוסרו. המודל אינו מחזיר מספרים
// יותר — את הציון קובעים Roslyn, מריץ הקוד ו-ScoreCalculator, והמודל תורם רק את ההסבר
// בעברית. לכן כל מספר שמוצג כאן ניתן לשחזור, ואפשר להתווכח איתו מול הקוד.
@Component({
  selector: "app-submission-feedback-panel",
  standalone: true,
  imports: [CommonModule, PanelModule, TableModule, TabViewModule, TagModule],
  templateUrl: "./submission-feedback-panel.component.html",
  styleUrls: ["./submission-feedback-panel.component.css"],
})
export class SubmissionFeedbackPanelComponent {
  @Input() submission: SubmissionResponseDto | null = null;

  /**
   * מי מסתכל. אותו רכיב, אותו תוכן — שני מצבי פתיחה.
   * <p>
   * התלמידה הגיעה להבין מספר אחד, ולכן נפתחים לה הציון וההסבר בלבד; הדרישות
   * והבדיקות מקופלות ונפתחות בקליק. המורה קוראת את המסך מלמעלה למטה כדי לאבחן,
   * ולכן אצלה הכול פתוח.
   * </p>
   * <p>
   * ⚠️ <b>מקופל, לעולם לא מוסר.</b> כשדרישה חוסמת נכשלת אין ציון כלל (‎G-1‎),
   * וטבלת הדרישות היא הדבר היחיד במסך שאומר איזו. הסרתה הייתה משאירה את התלמידה
   * מול «אין ציון» בלי סיבה — בדיוק הכישלון שהמסך הזה קיים כדי למנוע.
   * </p>
   */
  @Input() viewer: "student" | "teacher" = "teacher";

  expandedRows: Record<number, boolean> = {};

  /**
   * ⚠️ החריג לברירת המחדל של התלמידה: כשדרישה חוסמת נכשלה, הטבלה אינה פרט —
   * היא <i>ההסבר לציון</i>, ולכן נפתחת. לקפל אותה כאן היה מציג «אין ציון» בלי טעם.
   */
  get requirementsOpen(): boolean {
    if (this.viewer === "teacher") return true;
    return (this.submission?.structuralResults ?? []).some((r) =>
      this.isBlockingFailure(r),
    );
  }

  get testResultsOpen(): boolean {
    return this.viewer === "teacher";
  }

  severityLabel(severity: RuleSeverity): string {
    return RULE_SEVERITY_LABELS_HE[severity] ?? severity;
  }

  /** הדרישה שבגללה אין ציון כלל — מסומנת ולא מוצגת כשורה שווה בין שורות. */
  isBlockingFailure(rule: StructuralRuleResultDto): boolean {
    return rule.severity === "Blocking" && !rule.passed;
  }

  /**
   * ניקוד הדרישה. ⚠️ בינארי בכוונה: דרישה היא תנאי ולא מדידה — אין ניקוד חלקי על
   * "לכל היותר 3 if" כשנכתבו 4.
   */
  rulePointsLabel(rule: StructuralRuleResultDto): string {
    if (rule.severity !== "Scored") return "—";
    return `${rule.passed ? rule.points : 0} מתוך ${rule.points}`;
  }

  ngOnChanges(): void {
    // הרחבה כברירת מחדל רק לטסטים שנכשלו — עוברים ניתנים להרחבה לפי דרישה.
    // שורה מוסתרת לעולם לא נפתחת: אין לה תוכן להציג.
    const results = this.submission?.testResults ?? [];
    this.expandedRows = results.reduce((acc, t, i) => {
      if (!t.passed && !t.isHidden) acc[i] = true;
      return acc;
    }, {} as Record<number, boolean>);
  }

  get passedCount(): number {
    return (this.submission?.testResults ?? []).filter((t) => t.passed).length;
  }

  get totalCount(): number {
    return (this.submission?.testResults ?? []).length;
  }

  get failedCount(): number {
    return this.totalCount - this.passedCount;
  }

  get passedPercent(): number {
    return this.totalCount === 0 ? 0 : (this.passedCount / this.totalCount) * 100;
  }

  // מוסיף מונה לכותרת טאב רק כשיש בו הערות, כדי לא לזרוע "(0)" על פני כל הטאבים
  countSuffix(items: unknown[] | null | undefined): string {
    const count = items?.length ?? 0;
    return count > 0 ? ` (${count})` : "";
  }

  /**
   * "Wrong Answer" הוא בדיוק מה שהתג כבר אומר — מוצג רק סטטוס שמוסיף מידע, כלומר קריסה
   * או חריגת זמן.
   */
  failureReason(test: TestCaseResultDto): string | null {
    const status = test.statusDescription;
    if (!status || status === "Wrong Answer" || status === "Accepted") return null;
    return status;
  }

  toggleRow(index: number): void {
    this.expandedRows = {
      ...this.expandedRows,
      [index]: !this.expandedRows[index],
    };
  }
}
