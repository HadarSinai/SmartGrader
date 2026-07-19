using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Classes.DeleteClass
{
    public class DeleteClassHandler : IRequestHandler<DeleteClassCommand>
    {
        private readonly ISchoolClassRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteClassHandler(ISchoolClassRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteClassCommand request, CancellationToken cancellationToken)
        {
            var schoolClass = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (schoolClass is null)
                throw new NotFoundException(nameof(SchoolClass), request.Id);

            if (schoolClass.Students.Count > 0)
                throw new BusinessRuleException("לא ניתן למחוק כיתה שיש בה תלמידים — יש להעביר לארכיון במקום");

            await _repository.DeleteAsync(schoolClass, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
