using SmartGrader.Application.Dtos.Assignments;
using SmartGrader.Application.Dtos.Submissions;

namespace SmartGrader.Application.Common.Authorization
{
    /// <summary>
    /// הסתרת תשובות מקרי הבדיקה מתלמידה — <b>בשרת בלבד</b>.
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
        /// משאיר לתלמידה רק את מקרי הדוגמה. מורה/מנהלת מקבלת את הרשימה המלאה.
        /// </summary>
        public static AssignmentResponseDto RedactTests(AssignmentResponseDto dto, bool isStudentCaller)
        {
            if (isStudentCaller)
                dto.Tests = dto.Tests.Where(t => t.IsSample).ToList();

            return dto;
        }

        public static IReadOnlyList<AssignmentResponseDto> RedactTests(
            IReadOnlyList<AssignmentResponseDto> dtos, bool isStudentCaller)
        {
            if (isStudentCaller)
                foreach (var dto in dtos)
                    RedactTests(dto, isStudentCaller);

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
