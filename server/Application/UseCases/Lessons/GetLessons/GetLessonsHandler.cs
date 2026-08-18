using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Lessons;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Lessons.GetLessons
{
    public class GetLessonsHandler
        : IRequestHandler<GetLessonsQuery, IReadOnlyList<LessonResponseDto>>
    {
        private readonly ILessonRepository _repository;
        private readonly IStudentRepository _studentRepository;
        private readonly IMapper _mapper;

        public GetLessonsHandler(
            ILessonRepository repository,
            IStudentRepository studentRepository,
            IMapper mapper)
        {
            _repository = repository;
            _studentRepository = studentRepository;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<LessonResponseDto>> Handle(
            GetLessonsQuery request,
            CancellationToken cancellationToken)
        {
            // ⚠️ שני מסלולים בלעדיים זה לזה, לא if-ים מחוברים בשרשרת: תלמידה רואה רק את השיעורים
            // של הכיתה שלה, ולעולם לא מסונן לפי TeacherId — אחרת עלולה לראות אפס שיעורים אם המסנן
            // מוחל בטעות גם כשקוראים בשם תלמידה. מורה/מנהל לעולם לא מסונן לפי כיתת תלמידה.
            if (request.StudentId.HasValue)
            {
                var student = await _studentRepository.GetByIdAsync(request.StudentId.Value, cancellationToken);
                if (student is null)
                    throw new NotFoundException(nameof(Student), request.StudentId.Value);

                var studentLessons = await _repository.GetAllAsync(student.ClassId, teacherId: null, cancellationToken);
                return MapToReadOnly(studentLessons);
            }

            var lessons = await _repository.GetAllAsync(request.ClassId, request.TeacherId, cancellationToken);
            return MapToReadOnly(lessons);
        }

        private IReadOnlyList<LessonResponseDto> MapToReadOnly(IReadOnlyList<Lesson> lessons)
        {
            // ✅ אם אין שיעורים – מחזירים אוסף ריק, לא null
            if (lessons == null || lessons.Count == 0)
                return Array.Empty<LessonResponseDto>();

            // ✅ נמפה לרשימה, ואז נהפוך אותה ל־ReadOnly
            var dtoList = _mapper.Map<List<LessonResponseDto>>(lessons);

            return dtoList.AsReadOnly();
        }
    }
}
