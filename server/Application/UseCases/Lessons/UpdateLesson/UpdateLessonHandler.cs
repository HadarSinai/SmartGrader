using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Lessons;
using SmartGrader.Application.UseCases.Lessons.CreateLesson;
using SmartGrader.Domain.Abstractions;

namespace SmartGrader.Application.UseCases.Lessons.UpdateLesson
{
    public class UpdateLessonHandler
        : IRequestHandler<UpdateLessonCommand, LessonResponseDto>
    {
        private readonly ILessonRepository _repository;
        private readonly ISchoolClassRepository _classRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UpdateLessonHandler(
            ILessonRepository repository,
            ISchoolClassRepository classRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _classRepository = classRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<LessonResponseDto> Handle(
            UpdateLessonCommand request,
            CancellationToken cancellationToken)
        {
            var lesson = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (lesson is null)
                throw new NotFoundException("Lesson", request.Id);

            var classes = await CreateLessonHandler.ResolveActiveClassesAsync(
                _classRepository, request.Dto.ClassIds, cancellationToken);

            // ⭐ מקצועי יותר — מיפוי DTO → Entity
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
