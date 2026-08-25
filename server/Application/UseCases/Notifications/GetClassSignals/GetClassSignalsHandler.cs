using MediatR;
using SmartGrader.Application.Dtos.Notifications;
using SmartGrader.Application.Services.Notifications;
using SmartGrader.Domain.Abstractions;

namespace SmartGrader.Application.UseCases.Notifications.GetClassSignals
{
    /// <summary>
    /// ⚠️ אין כאן AutoMapper ואין NotFoundException, בניגוד לרוב ה-handlers: אין ישות למפות
    /// וחלון בלי פעילות אינו שגיאה — הוא רשימה ריקה, וזו בדיוק התשובה שהדיג'סט מסתמך עליה
    /// כדי לא לשלוח מייל.
    /// </summary>
    public class GetClassSignalsHandler
        : IRequestHandler<GetClassSignalsQuery, IReadOnlyList<ClassSignalDto>>
    {
        private readonly ISubmissionRepository _repository;
        private readonly ClassSignalDetector _detector;

        public GetClassSignalsHandler(ISubmissionRepository repository, ClassSignalDetector detector)
        {
            _repository = repository;
            _detector = detector;
        }

        public async Task<IReadOnlyList<ClassSignalDto>> Handle(
            GetClassSignalsQuery request,
            CancellationToken cancellationToken)
        {
            var submissions = await _repository.GetConcludedInRangeAsync(
                request.FromUtc, request.ToUtc, request.TeacherId, cancellationToken);

            return _detector.Detect(submissions);
        }
    }
}
