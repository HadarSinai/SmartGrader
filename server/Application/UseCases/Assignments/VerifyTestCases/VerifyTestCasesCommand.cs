using MediatR;
using SmartGrader.Application.Dtos.Assignments;

namespace SmartGrader.Application.UseCases.Assignments.VerifyTestCases
{
    /// <summary>
    /// מריץ את הפתרון לדוגמה של המורה מול מקרי הבדיקה שבטופס ומחזיר תוצאה פר-מקרה.
    /// <para>
    /// ⚠️ <b>Command ולא Query למרות שהוא לא כותב כלום ל-DB</b> — הוא מפעיל הרצת קוד בתשלום
    /// ובעלת תופעות לוואי חיצוניות, ולכן אסור שייראה כקריאה חינמית שאפשר לחזור עליה.
    /// שום דבר לא נשמר: אין <c>Submission</c>, אין ציון, אין קריאה ל-AI.
    /// </para>
    /// <para>
    /// <c>TeacherId</c> מגיע מ-<c>OwnerScopeTeacherId</c> בבקר. בלעדיו כל מורה מריצה קוד
    /// בהקשר של שיעור של מורה אחרת — ר' LessonAccess.
    /// </para>
    /// </summary>
    public record VerifyTestCasesCommand(
        int LessonId,
        VerifyTestCasesRequestDto Dto,
        int? TeacherId
    ) : IRequest<VerifyTestCasesResultDto>;
}
