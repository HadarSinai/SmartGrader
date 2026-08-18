using AutoMapper;
using MediatR;
using SmartGrader.Application.Dtos.Courses;
using SmartGrader.Domain.Abstractions;

namespace SmartGrader.Application.UseCases.Courses.GetCourses
{
    public class GetCoursesHandler
        : IRequestHandler<GetCoursesQuery, IReadOnlyList<CourseResponseDto>>
    {
        private readonly ICourseRepository _repository;
        private readonly IMapper _mapper;

        public GetCoursesHandler(ICourseRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<CourseResponseDto>> Handle(
            GetCoursesQuery request,
            CancellationToken cancellationToken)
        {
            var courses = await _repository.GetAllAsync(request.TeacherId, cancellationToken);

            return _mapper.Map<IReadOnlyList<CourseResponseDto>>(courses);
        }
    }
}
