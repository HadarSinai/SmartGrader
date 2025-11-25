using AutoMapper;
using Domain.Abstractions;
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
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UpdateStudentHandler(
            IStudentRepository repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
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

            _mapper.Map(request.Dto, student);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<StudentResponseDto>(student);
        }
    }
}
