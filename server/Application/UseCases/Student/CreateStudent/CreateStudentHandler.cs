using AutoMapper;
using Domain.Abstractions;
using MediatR;
using SmartGrader.Application.Dtos.Student;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Students.CreateStudent
{
    public class CreateStudentHandler
        : IRequestHandler<CreateStudentCommand, StudentResponseDto>
    {
        private readonly IStudentRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CreateStudentHandler(
            IStudentRepository repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<StudentResponseDto> Handle(
            CreateStudentCommand request,
            CancellationToken cancellationToken)
        {
            // DTO → Entity
            var student = _mapper.Map<Student>(request.Dto);

            await _repository.AddAsync(student, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Entity → ResponseDto
            return _mapper.Map<StudentResponseDto>(student);
        }
    }
}
