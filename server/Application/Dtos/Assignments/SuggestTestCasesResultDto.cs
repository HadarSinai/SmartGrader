namespace SmartGrader.Application.Dtos.Assignments
{
    /// <summary>
    /// מקרה בדיקה מוצע. <b>הצעה בלבד</b> — שום דבר כאן לא נשמר עד שהמורה מאשרת בטופס.
    /// </summary>
    public class SuggestedTestCaseDto
    {
        public string Input { get; set; } = "";

        /// <summary>
        /// הפלט הצפוי <b>שייכתב לטופס</b>. כשיש פתרון לדוגמה זהו הפלט שהוא החזיר בפועל,
        /// ולא מה שהמודל חשב — ר' <see cref="AiExpected"/>.
        /// </summary>
        public string Expected { get; set; } = "";

        /// <summary>
        /// מה שהמודל הציע כפלט. שווה ל-<see cref="Expected"/> כשהם הסכימו או כשלא היה
        /// פתרון לאמת מולו. מוצג למורה רק כשיש מחלוקת.
        /// </summary>
        public string AiExpected { get; set; } = "";

        /// <summary>הסבר קצר מה המקרה בודק — מוצג למורה בחלון ההצעות, לא נשמר.</summary>
        public string? Why { get; set; }

        /// <summary>סיווג ליבה/קצה שהמודל הציע. ממלא מראש את TestCaseDto.IsCore, וניתן לשינוי.</summary>
        public bool IsCore { get; set; } = true;

        /// <summary>האם ההצעה הורצה בפועל מול הפתרון לדוגמה.</summary>
        public bool Verified { get; set; }

        /// <summary>
        /// המודל והפתרון לדוגמה נתנו תשובות שונות. <b>הפתרון ניצח</b> ו-<see cref="Expected"/>
        /// מחזיק את התוצאה שרצה; המחלוקת מוצגת למורה במקום להיעלם.
        /// </summary>
        public bool Disagreed { get; set; }

        /// <summary>למה ההצעה לא אומתה (שגיאת ריצה, חריגת זמן) — כשהיא רצה אבל לא הצליחה.</summary>
        public string? VerificationError { get; set; }
    }

    public class SuggestTestCasesResultDto
    {
        public List<SuggestedTestCaseDto> Cases { get; set; } = new();

        /// <summary>
        /// האם ההצעות עברו הרצה מול פתרון לדוגמה. <c>false</c> = כל שורה היא ניחוש של מודל
        /// שאיש לא בדק, והלקוח מציג את האזהרה בהתאם.
        /// </summary>
        public bool Verified { get; set; }

        /// <summary>הודעה למורה כשהאימות לא התאפשר (אין פתרון / הפתרון לא מתקמפל).</summary>
        public string? Warning { get; set; }
    }
}
