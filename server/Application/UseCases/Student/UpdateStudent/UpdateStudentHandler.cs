using AutoMapper;

using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Student;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Students.UpdateStudent
{
    public class UpdateStudentHandler
        : IRequestHandler<UpdateStudentCommand, StudentResponseDto>
    {
        private readonly IStudentRepository _repository;
        private readonly ISchoolClassRepository _classRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UpdateStudentHandler(
            IStudentRepository repository,
            ISchoolClassRepository classRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _classRepository = classRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<StudentResponseDto> Handle(
            UpdateStudentCommand request,
            CancellationToken cancellationToken)
        {
            var student = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (student is null)
                throw new NotFoundException(nameof(Student), request.Id);

            if (request.Dto.ClassId != student.ClassId)
            {
                var schoolClass = await _classRepository.GetByIdAsync(request.Dto.ClassId, cancellationToken);
                if (schoolClass is null)
                    throw new NotFoundException(nameof(SchoolClass), request.Dto.ClassId);
                if (schoolClass.IsArchived)
                    throw new BusinessRuleException("לא ניתן לשייך תלמיד לכיתה בארכיון");
            }

            _mapper.Map(request.Dto, student);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<StudentResponseDto>(student);
        }
    }
}
