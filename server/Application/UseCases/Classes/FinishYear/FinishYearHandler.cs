using MediatR;
using SmartGrader.Domain.Abstractions;

namespace SmartGrader.Application.UseCases.Classes.FinishYear
{
    public class FinishYearHandler : IRequestHandler<FinishYearCommand, int>
    {
        private readonly ISchoolClassRepository _repository;

        public FinishYearHandler(ISchoolClassRepository repository)
        {
            _repository = repository;
        }

        public async Task<int> Handle(FinishYearCommand request, CancellationToken cancellationToken)
        {
            // ExecuteUpdate — הכתיבה מתבצעת ישירות מול בסיס הנתונים
            return await _repository.ArchiveAllActiveAsync(cancellationToken);
        }
    }
}
