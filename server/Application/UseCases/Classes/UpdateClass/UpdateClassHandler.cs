using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Classes;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Classes.UpdateClass
{
    public class UpdateClassHandler
        : IRequestHandler<UpdateClassCommand, SchoolClassResponseDto>
    {
        private readonly ISchoolClassRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UpdateClassHandler(
            ISchoolClassRepository repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<SchoolClassResponseDto> Handle(
            UpdateClassCommand request,
            CancellationToken cancellationToken)
        {
            var schoolClass = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (schoolClass is null)
                throw new NotFoundException(nameof(SchoolClass), request.Id);

            var duplicate = await _repository.GetByNameAndYearAsync(
                request.Dto.Name, request.Dto.AcademicYear, cancellationToken);

            if (duplicate is not null && duplicate.Id != request.Id)
                throw new UniqueConstraintException("כיתה בשם זה כבר קיימת בשנה זו");

            schoolClass.Name = request.Dto.Name;
            schoolClass.AcademicYear = request.Dto.AcademicYear;

            await _repository.UpdateAsync(schoolClass, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<SchoolClassResponseDto>(schoolClass);
        }
    }
}
