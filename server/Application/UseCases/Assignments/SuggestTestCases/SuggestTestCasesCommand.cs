using MediatR;
using SmartGrader.Application.Dtos.Assignments;

namespace SmartGrader.Application.UseCases.Assignments.SuggestTestCases
{
    /// <summary>
    /// מבקש מהמודל מקרי בדיקה, <b>מריץ כל אחד מהם מול הפתרון לדוגמה</b>, ומחזיר רשימת הצעות
    /// לסקירת המורה. שום דבר לא נשמר — ההצעות נכנסות לטופס רק אם היא מסמנת ומאשרת.
    /// </summary>
    public record SuggestTestCasesCommand(
        int LessonId,
        SuggestTestCasesRequestDto Dto,
        int? TeacherId
    ) : IRequest<SuggestTestCasesResultDto>;
}
