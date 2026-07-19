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
            var classId = request.ClassId;

            // תלמידה רואה רק שיעורים המשויכים לכיתה שלה
            if (request.StudentId.HasValue)
            {
                var student = await _studentRepository.GetByIdAsync(request.StudentId.Value, cancellationToken);
                if (student is null)
                    throw new NotFoundException(nameof(Student), request.StudentId.Value);

                classId = student.ClassId;
            }

            var lessons = await _repository.GetAllAsync(classId, cancellationToken);

            // ✅ אם אין שיעורים – מחזירים אוסף ריק, לא null
            if (lessons == null || lessons.Count == 0)
                return Array.Empty<LessonResponseDto>();

            // ✅ נמפה לרשימה, ואז נהפוך אותה ל־ReadOnly
            var dtoList = _mapper.Map<List<LessonResponseDto>>(lessons);

            return dtoList.AsReadOnly();
        }
    }
}
