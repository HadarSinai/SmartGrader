/**
 * מחיקה מרובה. תואם ל-BulkDeleteRequestDto / BulkDeleteResultDto בשרת.
 *
 * ⚠️ הצלחה חלקית היא התוצאה הרגילה ולא מקרה קצה — ר' B-55. מסך שמציג רק "נמחקו 6"
 * מסתיר ארבעה סירובים מאחורי הודעה ירוקה, ומורה שלא ראתה אותם תחזור לחפש למה השורות
 * עדיין שם.
 */
export interface BulkDeleteRequestDto {
  ids: number[];
}

export interface BulkDeleteFailureDto {
  id: number;
  /** הסיבה, כלשונה מהמחיקה הבודדת. */
  message: string;
}

export interface BulkDeleteResultDto {
  deletedIds: number[];
  failures: BulkDeleteFailureDto[];
  deletedCount: number;
  failedCount: number;
}

/** שורה מוכנה להצגה: הסיבה עם השם שהמורה מכירה, ולא עם מזהה. */
export interface BulkDeleteFailureRow {
  name: string;
  message: string;
}
