using AutoMapper;

using MediatR;
using SmartGrader.Application.Common.Authorization;
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
        private readonly ILessonRepository _lessons;
        private readonly IMapper _mapper;

        public GetStudentsHandler(
            IStudentRepository repository,
            ILessonRepository lessons,
            IMapper mapper)
        {
            _repository = repository;
            _lessons = lessons;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<StudentResponseDto>> Handle(
            GetStudentsQuery request,
            CancellationToken cancellationToken)
        {
            // ⚠️ לא GetAllAsync: היא מחזירה את כל תלמידות בית הספר לכל מורה מחוברת.
            // ר' StudentScope.
            var students = await StudentScope.GetVisibleAsync(
                _repository,
                _lessons,
                request.TeacherId,
                request.IncludeArchived,
                includeCounts: true,
                cancellationToken);

            return _mapper.Map<IReadOnlyList<StudentResponseDto>>(students);
        }
    }
}
