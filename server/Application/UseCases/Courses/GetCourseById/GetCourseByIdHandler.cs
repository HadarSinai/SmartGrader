using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Courses;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Courses.GetCourseById
{
    public class GetCourseByIdHandler
        : IRequestHandler<GetCourseByIdQuery, CourseResponseDto>
    {
        private readonly ICourseRepository _repository;
        private readonly IMapper _mapper;

        public GetCourseByIdHandler(ICourseRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<CourseResponseDto> Handle(
            GetCourseByIdQuery request,
            CancellationToken cancellationToken)
        {
            var course = await _repository.GetByIdAsync(request.Id, cancellationToken);

            // "לא נמצא" ו"לא שייך למורה" מוצגים זהה בכוונה — 404 לשניהם, לא 403, כדי לא לחשוף קיום.
            if (course is null || (request.TeacherId.HasValue && course.TeacherId != request.TeacherId.Value))
                throw new NotFoundException(nameof(Course), request.Id);

            return _mapper.Map<CourseResponseDto>(course);
        }
    }
}
