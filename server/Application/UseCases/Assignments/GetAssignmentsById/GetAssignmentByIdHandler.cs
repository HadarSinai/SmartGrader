using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Authorization;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Assignments;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Assignments.GetAssignmentById
{
    public class GetAssignmentByIdHandler
        : IRequestHandler<GetAssignmentByIdQuery, AssignmentResponseDto>
    {
        private readonly IAssignmentRepository _repository;
        private readonly ILessonRepository _lessonRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IMapper _mapper;

        public GetAssignmentByIdHandler(
            IAssignmentRepository repository,
            ILessonRepository lessonRepository,
            IStudentRepository studentRepository,
            IMapper mapper)
        {
            _repository = repository;
            _lessonRepository = lessonRepository;
            _studentRepository = studentRepository;
            _mapper = mapper;
        }

        public async Task<AssignmentResponseDto> Handle(
            GetAssignmentByIdQuery request,
            CancellationToken cancellationToken)
        {
            await LessonAccess.GetAccessibleOrThrowAsync(
                _lessonRepository, _studentRepository, request.LessonId, request.TeacherId, request.StudentId, cancellationToken);

            var assignment = await _repository.GetByIdAsync(request.AssignmentId, cancellationToken);

            if (assignment is null || assignment.LessonId != request.LessonId)
                throw new NotFoundException(nameof(Assignment), request.AssignmentId);

            // ⚠️ מקרי בדיקה שאינם דוגמה מכילים את התשובה לתרגיל — נחתכים כאן, בשרת, לפני
            // שה-DTO עוזב את ה-handler. ר' TestVisibility.
            return TestVisibility.RedactTests(
                _mapper.Map<AssignmentResponseDto>(assignment),
                request.IsStudentCaller);
        }
    }
}
