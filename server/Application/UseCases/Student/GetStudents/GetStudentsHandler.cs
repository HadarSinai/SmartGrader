using AutoMapper;
using Domain.Abstractions;
using MediatR;
using SmartGrader.Application.Dtos.Student;
using SmartGrader.Domain.Abstractions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmartGrader.Application.UseCases.Students.GetStudents
{
    public class GetStudentsHandler
        : IRequestHandler<GetStudentsQuery, IReadOnlyList<StudentResponseDto>>
    {
        private readonly IStudentRepository _repository;
        private readonly IMapper _mapper;

        public GetStudentsHandler(IStudentRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<StudentResponseDto>> Handle(
            GetStudentsQuery request,
            CancellationToken cancellationToken)
        {
            var students = await _repository.GetAllAsync(cancellationToken);

            return _mapper.Map<IReadOnlyList<StudentResponseDto>>(students);
        }
    }
}
