using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Courses;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Courses.UpdateCourse
{
    public class UpdateCourseHandler
        : IRequestHandler<UpdateCourseCommand, CourseResponseDto>
    {
        private readonly ICourseRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UpdateCourseHandler(
            ICourseRepository repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<CourseResponseDto> Handle(
            UpdateCourseCommand request,
            CancellationToken cancellationToken)
        {
            var course = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (course is null || (request.TeacherId.HasValue && course.TeacherId != request.TeacherId.Value))
                throw new NotFoundException(nameof(Course), request.Id);

            var duplicate = await _repository.GetByNameAndTeacherAsync(
                request.Dto.Name, course.TeacherId, cancellationToken);

            if (duplicate is not null && duplicate.Id != request.Id)
                throw new UniqueConstraintException("קורס בשם זה כבר קיים");

            course.Name = request.Dto.Name;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<CourseResponseDto>(course);
        }
    }
}
