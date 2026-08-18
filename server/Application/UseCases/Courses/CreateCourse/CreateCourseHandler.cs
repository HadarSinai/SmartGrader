using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Courses;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Courses.CreateCourse
{
    public class CreateCourseHandler
        : IRequestHandler<CreateCourseCommand, CourseResponseDto>
    {
        private readonly ICourseRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CreateCourseHandler(
            ICourseRepository repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<CourseResponseDto> Handle(
            CreateCourseCommand request,
            CancellationToken cancellationToken)
        {
            var existing = await _repository.GetByNameAndTeacherAsync(
                request.Dto.Name, request.TeacherId, cancellationToken);

            if (existing is not null)
                throw new UniqueConstraintException("קורס בשם זה כבר קיים");

            var course = Course.Create(request.Dto.Name, request.TeacherId);

            await _repository.AddAsync(course, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<CourseResponseDto>(course);
        }
    }
}
