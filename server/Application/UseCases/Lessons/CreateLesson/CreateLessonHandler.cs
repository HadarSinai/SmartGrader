using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Lessons;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Lessons.CreateLesson
{
    public class CreateLessonHandler
        : IRequestHandler<CreateLessonCommand, LessonResponseDto>
    {
        private readonly ILessonRepository _repository;
        private readonly ISchoolClassRepository _classRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CreateLessonHandler(
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
            CreateLessonCommand request,
            CancellationToken cancellationToken)
        {
            var classes = await ResolveActiveClassesAsync(
                _classRepository, request.Dto.ClassIds, cancellationToken);

            var lesson = _mapper.Map<Lesson>(request.Dto);
            foreach (var schoolClass in classes)
                lesson.Classes.Add(schoolClass);

            await _repository.AddAsync(lesson, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<LessonResponseDto>(lesson);
        }

        // משותף גם ל-UpdateLessonHandler: כל הכיתות חייבות להתקיים ולהיות פעילות
        internal static async Task<IReadOnlyList<SchoolClass>> ResolveActiveClassesAsync(
            ISchoolClassRepository classRepository,
            List<int> classIds,
            CancellationToken ct)
        {
            var distinctIds = classIds.Distinct().ToList();
            var classes = await classRepository.GetByIdsAsync(distinctIds, ct);

            if (classes.Count != distinctIds.Count)
            {
                var missing = distinctIds.Except(classes.Select(c => c.Id)).First();
                throw new NotFoundException(nameof(SchoolClass), missing);
            }

            if (classes.Any(c => c.IsArchived))
                throw new BusinessRuleException("לא ניתן לשייך שיעור לכיתה בארכיון");

            return classes;
        }
    }
}
