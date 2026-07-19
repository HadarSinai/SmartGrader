using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Classes;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Classes.CreateClass
{
    public class CreateClassHandler
        : IRequestHandler<CreateClassCommand, SchoolClassResponseDto>
    {
        private readonly ISchoolClassRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CreateClassHandler(
            ISchoolClassRepository repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<SchoolClassResponseDto> Handle(
            CreateClassCommand request,
            CancellationToken cancellationToken)
        {
            var existing = await _repository.GetByNameAndYearAsync(
                request.Dto.Name, request.Dto.AcademicYear, cancellationToken);

            if (existing is not null)
                throw new UniqueConstraintException("כיתה בשם זה כבר קיימת בשנה זו");

            var schoolClass = SchoolClass.Create(request.Dto.Name, request.Dto.AcademicYear);

            await _repository.AddAsync(schoolClass, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<SchoolClassResponseDto>(schoolClass);
        }
    }
}
