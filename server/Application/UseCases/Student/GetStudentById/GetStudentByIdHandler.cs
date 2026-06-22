using AutoMapper;
using Domain.Abstractions;
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Student;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Students.GetStudentById
{
    public class GetStudentByIdHandler
        : IRequestHandler<GetStudentByIdQuery, StudentResponseDto>
    {
        private readonly IStudentRepository _repository;
        private readonly IMapper _mapper;

        public GetStudentByIdHandler(IStudentRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<StudentResponseDto> Handle(
            GetStudentByIdQuery request,
            CancellationToken cancellationToken)
        {
            var student = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (student is null)
                throw new NotFoundException(nameof(Student), request.Id);

            return _mapper.Map<StudentResponseDto>(student);
        }
    }
}
