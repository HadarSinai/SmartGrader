using SmartGrader.Application.Dtos.Assignments;
using SmartGrader.Application.Dtos.Submissions;

namespace SmartGrader.Application.Common.Authorization
{
    /// <summary>
    /// הסתרת התשובה לתרגיל מתלמידה — <b>בשרת בלבד</b>. שני נתיבים: מקרי בדיקה מוסתרים,
    /// והפתרון לדוגמה של המורה.
    /// <para>
    /// מקרה בדיקה שאינו דוגמה (<c>IsSample == false</c>) מכיל את התשובה לתרגיל. הסתרה בתבנית
    /// Angular היא חסרת ערך: ה-payload כבר הגיע לדפדפן ונקרא ב-DevTools בשלמותו. לכן הסינון
    /// קורה כאן, על ה-DTO, לפני שהוא עוזב את ה-handler.
    /// </para>
    /// <para>
    /// תפקיד הקורא (תלמידה או מורה/מנהלת) נקבע בגבול ה-Controller ומועבר לתוך ה-Query —
    /// אין בדיקת <c>User</c> בתוך handler. שימי לב ש-<c>TeacherIdForSharedRead is null</c>
    /// אינו מזהה תלמידה: הוא null גם עבור מנהלת. הסימן הנכון הוא StudentId/IsStudentCaller.
    /// </para>
    /// </summary>
    public static class TestVisibility
    {
        /// <summary>
        /// משאיר לתלמידה רק את מקרי הדוגמה, ומרוקן את הפתרון לדוגמה.
        /// מורה/מנהלת מקבלת את התרגיל המלא.
        /// <para>
        /// שתי ההסתרות באותה פונקציה בכוונה: שתיהן מסתירות את התשובה לתרגיל, והפרדתן לשתי
        /// קריאות נפרדות פירושה שכל endpoint עתידי יצטרך לזכור להוסיף שתיים — וישכח את השנייה.
        /// </para>
        /// </summary>
        public static AssignmentResponseDto Redact(AssignmentResponseDto dto, bool isStudentCaller)
        {
            if (!isStudentCaller)
                return dto;

            dto.Tests = dto.Tests.Where(t => t.IsSample).ToList();

            // ⚠️ הפתרון לדוגמה הוא התשובה המלאה — לא "רוב התשובה", לא "רמז". מרוקן, לא מסונן.
            dto.ReferenceSolution = new List<ReferenceSolutionFileDto>();

            return dto;
        }

        public static IReadOnlyList<AssignmentResponseDto> Redact(
            IReadOnlyList<AssignmentResponseDto> dtos, bool isStudentCaller)
        {
            if (isStudentCaller)
                foreach (var dto in dtos)
                    Redact(dto, isStudentCaller);

            return dtos;
        }

        /// <summary>
        /// מרוקן את פרטי התוצאה של מקרה בדיקה מוסתר, ומשאיר רק את <c>Passed</c> — כך שהסיכום
        /// "עברו 3 מתוך 5" נשמר בלי לחשוף דבר.
        /// <para>
        /// גם <c>Actual</c> וגם <c>Error</c> מרוקנים, ולא רק Input/Expected: התלמידה שולטת בקוד
        /// שרץ על הקלט המוסתר, ולכן הדפסה שלו ל-stdout או ל-stderr הייתה מחזירה לה את הקלט
        /// בעצמה דרך השדות האלה.
        /// </para>
        /// </summary>
        public static SubmissionResponseDto RedactTestResults(SubmissionResponseDto dto, bool isStudentCaller)
        {
            if (!isStudentCaller)
                return dto;

            foreach (var result in dto.TestResults.Where(r => !r.IsSample))
            {
                result.IsHidden = true;
                result.Input = "";
                result.Expected = "";
                result.Actual = "";
                result.Error = null;
            }

            return dto;
        }

        public static IReadOnlyList<SubmissionResponseDto> RedactTestResults(
            IReadOnlyList<SubmissionResponseDto> dtos, bool isStudentCaller)
        {
            if (isStudentCaller)
                foreach (var dto in dtos)
                    RedactTestResults(dto, isStudentCaller);

            return dtos;
        }
    }
}
