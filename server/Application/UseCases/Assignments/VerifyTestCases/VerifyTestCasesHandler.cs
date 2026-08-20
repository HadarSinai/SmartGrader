using System.Text.Json;
using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Authorization;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Assignments;
using SmartGrader.Application.Services.CodeRunner;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Assignments.VerifyTestCases
{
    public class VerifyTestCasesHandler
        : IRequestHandler<VerifyTestCasesCommand, VerifyTestCasesResultDto>
    {
        private readonly ILessonRepository _lessonRepository;
        private readonly ICodeRunnerService _codeRunner;
        private readonly IMapper _mapper;

        public VerifyTestCasesHandler(
            ILessonRepository lessonRepository,
            ICodeRunnerService codeRunner,
            IMapper mapper)
        {
            _lessonRepository = lessonRepository;
            _codeRunner = codeRunner;
            _mapper = mapper;
        }

        public async Task<VerifyTestCasesResultDto> Handle(
            VerifyTestCasesCommand request,
            CancellationToken cancellationToken)
        {
            // ⚠️ בעלות. [Authorize(Roles)] בלבד מתיר לכל מורה במערכת להריץ קוד בהקשר של
            // שיעור של מורה אחרת — בדיוק ההשמטה שתוארה ב-CompleteLesson.
            await LessonAccess.GetOwnedOrThrowAsync(
                _lessonRepository, request.LessonId, request.TeacherId, cancellationToken);

            var runnerResult = await RunReferenceSolutionAsync(request.Dto, cancellationToken);

            return BuildResult(runnerResult, request.Dto.Tests.Count);
        }

        /// <summary>
        /// מריץ את הפתרון לדוגמה דרך <see cref="GradingModeRunner"/> — אותו מסלול בדיוק
        /// שבו תיבדק ההגשה של התלמידה. זה כל הערך של התכונה: אימות שרץ במסלול אחר מהניקוד
        /// נותן ביטחון שקרי.
        /// </summary>
        internal static async Task<RunnerResult> RunAsync(
            ICodeRunnerService codeRunner,
            IMapper mapper,
            string gradingMode,
            string? methodName,
            IReadOnlyList<ReferenceSolutionFileDto> referenceSolution,
            IReadOnlyList<ExpectedFileDto> expectedFiles,
            IReadOnlyList<TestCaseDto> tests,
            CancellationToken ct)
        {
            // תקף כי ה-Validator בדק IsEnumName לפני שה-Handler רץ
            var mode = Enum.Parse<GradingMode>(gradingMode, true);

            // הפתרון של המורה נכנס למקום שבו יושב קוד התלמידה בהרצה אמיתית.
            var sourceFiles = referenceSolution
                .Where(f => !string.IsNullOrWhiteSpace(f.Content))
                .Select(f => new SubmissionFile
                {
                    FileName = string.IsNullOrWhiteSpace(f.FileName) ? "Solution.cs" : f.FileName,
                    Content = f.Content,
                })
                .ToList();

            try
            {
                return await GradingModeRunner.RunAsync(
                    codeRunner,
                    mode,
                    sourceFiles,
                    sourceCode: null,
                    methodName ?? "",
                    mapper.Map<List<ExpectedFile>>(expectedFiles),
                    mapper.Map<List<TestCase>>(tests),
                    ct);
            }
            catch (Exception ex) when (ex is HttpRequestException
                or JsonException
                or TaskCanceledException
                or CodeRunnerUnavailableException)
            {
                // כשל תשתית של Judge0 — לא באג בפתרון של המורה ולא במקרי הבדיקה. הודעה
                // מפורשת, כי "הבדיקה נכשלה" סתם היה שולח את המורה לחפש טעות שאינה קיימת.
                throw new BusinessRuleException(
                    "מערכת בדיקת הקוד אינה זמינה כרגע, ולכן אי אפשר לבדוק את מקרי הבדיקה. " +
                    "אפשר לשמור את התרגיל ולנסות שוב מאוחר יותר.");
            }
        }

        private Task<RunnerResult> RunReferenceSolutionAsync(
            VerifyTestCasesRequestDto dto, CancellationToken ct) =>
            RunAsync(
                _codeRunner, _mapper,
                dto.GradingMode, dto.MethodName,
                dto.ReferenceSolution, dto.ExpectedFiles, dto.Tests,
                ct);

        private static VerifyTestCasesResultDto BuildResult(RunnerResult runnerResult, int totalTests)
        {
            var result = new VerifyTestCasesResultDto
            {
                Passed = runnerResult.Passed,
                Total = runnerResult.Total > 0 ? runnerResult.Total : totalTests,
                HasCompileError = runnerResult.HasCompileError,
                CompileError = runnerResult.CompileError,
            };

            // בכשל קומפילציה Details ריקה — הרשימה נשארת ריקה והלקוח מציג רק את השגיאה.
            result.Results = runnerResult.Details
                .Select((d, i) => new TestCaseVerificationDto
                {
                    Index = i,
                    Input = d.Input,
                    Expected = d.Expected,
                    Actual = d.Actual,
                    Passed = d.Passed,
                    Error = d.Error,
                    StatusDescription = d.StatusDescription,
                    CanFix = CanOfferFix(d),
                })
                .ToList();

            return result;
        }

        /// <summary>
        /// האם מותר להציע למורה "תיקון ל-X".
        /// <para>
        /// ⚠️ שגיאת ריצה או חריגת זמן מחזירות <c>Actual</c> ריק. הצעת תיקון במצב הזה הייתה
        /// כותבת מחרוזת ריקה לשדה "פלט צפוי" ובכך הופכת מקרה בדיקה תקין למקרה שמצפה לכלום —
        /// והמורה הייתה מאשרת את זה בלחיצה אחת בלי לשים לב. לכן שני התנאים, וכיוון הכשל
        /// הבטוח הוא לא להציע.
        /// </para>
        /// </summary>
        private static bool CanOfferFix(TestCaseResult detail) =>
            detail.Error is null && !string.IsNullOrWhiteSpace(detail.Actual);
    }
}
