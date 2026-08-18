using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Authorization;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Lessons;
using SmartGrader.Application.UseCases.Lessons.CreateLesson;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Lessons.UpdateLesson
{
    public class UpdateLessonHandler
        : IRequestHandler<UpdateLessonCommand, LessonResponseDto>
    {
        private readonly ILessonRepository _repository;
        private readonly ISchoolClassRepository _classRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UpdateLessonHandler(
            ILessonRepository repository,
            ISchoolClassRepository classRepository,
            ICourseRepository courseRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _classRepository = classRepository;
            _courseRepository = courseRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<LessonResponseDto> Handle(
            UpdateLessonCommand request,
            CancellationToken cancellationToken)
        {
            // בעלות נאכפת כאן — ולא ב-GetByIdAsync גולמי כמו קודם.
            var lesson = await LessonAccess.GetOwnedOrThrowAsync(_repository, request.Id, request.TeacherId, cancellationToken);

            var classes = await CreateLessonHandler.ResolveActiveClassesAsync(
                _classRepository, request.Dto.ClassIds, cancellationToken);

            var course = await _courseRepository.GetByIdAsync(request.Dto.CourseId, cancellationToken);
            if (course is null || course.TeacherId != lesson.TeacherId)
                throw new NotFoundException(nameof(Course), request.Dto.CourseId);

            // ⭐ מקצועי יותר — מיפוי DTO → Entity
            // ⚠️ LessonProfile.CreateMap<UpdateLessonRequestDto, Lesson>() חייב .Ignore() על TeacherId —
            // אחרת השורה הזו מאפסת את הבעלות על השיעור בכל עדכון (R1).
            _mapper.Map(request.Dto, lesson);

            lesson.Classes.Clear();
            foreach (var schoolClass in classes)
                lesson.Classes.Add(schoolClass);

            // ⭐ אין צורך ב-UpdateAsync
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<LessonResponseDto>(lesson);
        }
    }
}
