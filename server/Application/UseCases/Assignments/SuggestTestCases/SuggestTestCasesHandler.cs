using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Authorization;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Common.Validation;
using SmartGrader.Application.Dtos.Assignments;
using SmartGrader.Application.Services.CodeRunner;
using SmartGrader.Application.Services.Feedback;
using SmartGrader.Application.UseCases.Assignments.VerifyTestCases;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Assignments.SuggestTestCases
{
    /// <summary>
    /// <b>המודל מציע, ההרצה קובעת, המורה מאשרת.</b>
    /// <para>
    /// במקומות אחרים במערכת ה-AI מורחק במכוון מכל מספר, כי ציון חייב להיות ניתן לשחזור
    /// ולהסבר. כאן המצב הפוך: הפלט של המודל <i>ניתן לאימות עצמאי לפני השימוש בו</i> —
    /// מריצים אותו מול הפתרון של המורה. לכן מותר לו להציע, ולכן אסור להאמין לו: כל מקום
    /// שבו המודל והפתרון חולקים, הפתרון מנצח והמחלוקת <b>מוצגת</b> ולא נבלעת.
    /// </para>
    /// </summary>
    public class SuggestTestCasesHandler
        : IRequestHandler<SuggestTestCasesCommand, SuggestTestCasesResultDto>
    {
        private readonly ILessonRepository _lessonRepository;
        private readonly ITestCaseSuggestionService _suggestions;
        private readonly ICodeRunnerService _codeRunner;
        private readonly IMapper _mapper;

        public SuggestTestCasesHandler(
            ILessonRepository lessonRepository,
            ITestCaseSuggestionService suggestions,
            ICodeRunnerService codeRunner,
            IMapper mapper)
        {
            _lessonRepository = lessonRepository;
            _suggestions = suggestions;
            _codeRunner = codeRunner;
            _mapper = mapper;
        }

        public async Task<SuggestTestCasesResultDto> Handle(
            SuggestTestCasesCommand request,
            CancellationToken cancellationToken)
        {
            await LessonAccess.GetOwnedOrThrowAsync(
                _lessonRepository, request.LessonId, request.TeacherId, cancellationToken);

            var dto = request.Dto;
            var mode = Enum.Parse<GradingMode>(dto.GradingMode, true);

            IReadOnlyList<SuggestedTestCase> proposals;
            try
            {
                proposals = await _suggestions.SuggestAsync(
                    Truncate(dto.Description, SuggestTestCasesLimits.MaxDescriptionLength),
                    mode,
                    dto.MethodName,
                    dto.Count,
                    cancellationToken);
            }
            catch (TestCaseSuggestionUnavailableException ex)
            {
                // 400 עם הודעה ברורה, לא 500. זו לא תקלה במערכת אלא שירות חיצוני שאינו זמין,
                // וכתיבה ידנית ואימות ממשיכים לעבוד בדיוק כמקודם.
                throw new BusinessRuleException(ex.Message);
            }

            if (proposals.Count == 0)
                return new SuggestTestCasesResultDto
                {
                    Cases = new List<SuggestedTestCaseDto>(),
                    Verified = false,
                    Warning = "לא התקבלו הצעות. אפשר לנסות שוב או להוסיף פירוט לתיאור התרגיל.",
                };

            var hasReferenceSolution = dto.ReferenceSolution
                .Any(f => !string.IsNullOrWhiteSpace(f.Content));

            if (!hasReferenceSolution)
                return Unverified(
                    proposals,
                    "אין פתרון לדוגמה, ולכן אף הצעה לא נבדקה. הפלטים כאן הם ניחוש של המודל — " +
                    "כדאי להוסיף פתרון לדוגמה ולבדוק לפני שמירה.");

            return await VerifyAgainstReferenceAsync(proposals, dto, cancellationToken);
        }

        /// <summary>
        /// 🔴 הצעד שהופך את התכונה לבטוחה: כל הצעה מורצת מול הפתרון של המורה, ומה שהפתרון
        /// החזיר הוא מה שנשמר. המודל הוא מקור ל<i>רעיונות לקלטים מעניינים</i>, לא מקור אמת
        /// לגבי פלטים.
        /// </summary>
        private async Task<SuggestTestCasesResultDto> VerifyAgainstReferenceAsync(
            IReadOnlyList<SuggestedTestCase> proposals,
            SuggestTestCasesRequestDto dto,
            CancellationToken ct)
        {
            // הפלט של המודל נכנס כ-Expected רק כדי שההשוואה המנורמלת של ה-Runner תעשה
            // את העבודה: Passed==false פירושו בדיוק "המודל והפתרון לא הסכימו".
            var candidateTests = proposals
                .Select(p => new TestCaseDto { Input = p.Input, Expected = p.Expected })
                .ToList();

            RunnerResult runnerResult;
            try
            {
                runnerResult = await VerifyTestCasesHandler.RunAsync(
                    _codeRunner, _mapper,
                    dto.GradingMode, dto.MethodName,
                    dto.ReferenceSolution, dto.ExpectedFiles, candidateTests,
                    ct);
            }
            catch (BusinessRuleException)
            {
                // ⚠️ ירידה מדורגת, לא כישלון: מערכת ההרצה נפלה אחרי שההצעות כבר התקבלו.
                // זריקת שגיאה כאן הייתה משליכה לפח עבודה ששולם עליה, ומשאירה את המורה בלי כלום.
                return Unverified(
                    proposals,
                    "ההצעות התקבלו, אבל מערכת בדיקת הקוד אינה זמינה כרגע ולכן אף אחת מהן לא נבדקה.");
            }

            if (runnerResult.HasCompileError)
                return Unverified(
                    proposals,
                    "הפתרון לדוגמה שלך לא עובר קומפילציה, ולכן אי אפשר לבדוק את ההצעות מולו: " +
                    (runnerResult.CompileError ?? "שגיאת קומפילציה"));

            var cases = new List<SuggestedTestCaseDto>();

            for (int i = 0; i < proposals.Count; i++)
            {
                var proposal = proposals[i];
                var detail = i < runnerResult.Details.Count ? runnerResult.Details[i] : null;

                // אותו תנאי בדיוק כמו CanOfferFix באימות: שגיאת ריצה או חריגת זמן מחזירות
                // פלט ריק, וכתיבתו כפלט צפוי הייתה יוצרת מקרה בדיקה שמצפה לכלום.
                var ran = detail is not null
                          && detail.Error is null
                          && !string.IsNullOrWhiteSpace(detail.Actual);

                cases.Add(new SuggestedTestCaseDto
                {
                    Input = proposal.Input,
                    // הפתרון מנצח כשהוא רץ; אחרת נשארת הצעת המודל, מסומנת כלא-אומתה.
                    Expected = ran ? detail!.Actual : proposal.Expected,
                    AiExpected = proposal.Expected,
                    Why = proposal.Why,
                    IsCore = proposal.IsCore,
                    Verified = ran,
                    Disagreed = ran && !detail!.Passed,
                    VerificationError = ran ? null : DescribeFailure(detail),
                });
            }

            var unverifiedCount = cases.Count(c => !c.Verified);

            return new SuggestTestCasesResultDto
            {
                Cases = cases,
                Verified = unverifiedCount == 0,
                Warning = unverifiedCount == 0
                    ? null
                    : $"{unverifiedCount} מתוך {cases.Count} ההצעות לא נבדקו — הפתרון לדוגמה לא סיים לרוץ עליהן.",
            };
        }

        private static string? DescribeFailure(Domain.Entities.TestCaseResult? detail)
        {
            if (detail is null)
                return "ההרצה נעצרה לפני המקרה הזה.";

            return detail.Error
                   ?? detail.StatusDescription
                   ?? "הפתרון לדוגמה לא החזיר פלט עבור הקלט הזה.";
        }

        /// <summary>הצעות שלא הורצו — כל שורה מסומנת "לא אומת" והאזהרה בולטת בלקוח.</summary>
        private static SuggestTestCasesResultDto Unverified(
            IReadOnlyList<SuggestedTestCase> proposals, string warning) =>
            new()
            {
                Cases = proposals.Select(p => new SuggestedTestCaseDto
                {
                    Input = p.Input,
                    Expected = p.Expected,
                    AiExpected = p.Expected,
                    Why = p.Why,
                    IsCore = p.IsCore,
                    Verified = false,
                    Disagreed = false,
                }).ToList(),
                Verified = false,
                Warning = warning,
            };

        private static string Truncate(string value, int maxLength) =>
            value.Length <= maxLength ? value : value[..maxLength];
    }
}
