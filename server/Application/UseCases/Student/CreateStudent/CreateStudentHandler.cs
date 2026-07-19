using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Student;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Students.CreateStudent
{
    public class CreateStudentHandler
        : IRequestHandler<CreateStudentCommand, StudentResponseDto>
    {
        private readonly IStudentRepository _repository;
        private readonly ISchoolClassRepository _classRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CreateStudentHandler(
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
            CreateStudentCommand request,
            CancellationToken cancellationToken)
        {
            var schoolClass = await _classRepository.GetByIdAsync(request.Dto.ClassId, cancellationToken);
            if (schoolClass is null)
                throw new NotFoundException(nameof(SchoolClass), request.Dto.ClassId);
            if (schoolClass.IsArchived)
                throw new BusinessRuleException("לא ניתן לשייך תלמיד לכיתה בארכיון");

            // DTO → Entity
            var student = _mapper.Map<Student>(request.Dto);

            await _repository.AddAsync(student, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Entity → ResponseDto
            return _mapper.Map<StudentResponseDto>(student);
        }
    }
}
