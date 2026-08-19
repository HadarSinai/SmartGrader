import { SubmissionResponseDto } from './submission.model';

export interface SubmissionExtended extends SubmissionResponseDto {
  codePreview?: string;
  executionTime?: number;
  memoryUsage?: number;
  language?: string;
  // ⚠️ evaluatedBy הוסר: הוא נגזר מ-`s.feedback ? "AI" : "Manual"`, אבל אין בכלל בדיקה ידנית
  // במערכת — כל ההגשות נבדקות ב-AI — והערך גם לא הוצג בשום מסך.
}
