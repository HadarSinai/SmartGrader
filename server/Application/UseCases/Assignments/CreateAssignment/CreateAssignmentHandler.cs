using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Authorization;
using SmartGrader.Application.Dtos.Assignments;
using SmartGrader.Application.Dtos.Lessons;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Assignments.CreateAssignment
{
    public class CreateAssignmentHandler
        : IRequestHandler<CreateAssignmentCommand, AssignmentResponseDto>
    {
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly ILessonRepository _lessonRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CreateAssignmentHandler(
            IAssignmentRepository assignmentRepository,
            ILessonRepository lessonRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _assignmentRepository = assignmentRepository;
            _lessonRepository = lessonRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<AssignmentResponseDto> Handle(
            CreateAssignmentCommand request,
            CancellationToken cancellationToken)
        {
            await LessonAccess.GetOwnedOrThrowAsync(_lessonRepository, request.LessonId, request.TeacherId, cancellationToken);

            var assignment = _mapper.Map<Assignment>(request.Dto);
            assignment.LessonId = request.LessonId;
            // הערה: AutoMapper כבר ממפה את Dto.Tests -> Assignment.Tests (ומעדכן TestsJson),
            // לכן אין להוסיף כאן את המקרים שוב ידנית - זה גרם לכפילות.

            // הפתרון לדוגמה דווקא כן נכתב ידנית — SetReferenceSolution זורק שורות בלי תוכן,
            // ומיפוי ישיר היה עוקף את הסינון. ר' ההערה ב-AssignmentProfile.
            assignment.SetReferenceSolution(
                _mapper.Map<List<ReferenceSolutionFile>>(request.Dto.ReferenceSolution ?? new()));

            // הדרישות המבניות נכתבות דרך ה-setter מאותו נימוק כמו הפתרון לדוגמה:
            // StructuralRulesJson מסומן Ignore במיפוי, ורק SetStructuralRules כותב אליו.
            assignment.SetStructuralRules(
                _mapper.Map<List<StructuralRule>>(request.Dto.StructuralRules ?? new()));

            await _assignmentRepository.AddAsync(assignment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<AssignmentResponseDto>(assignment);
        }
    }
}
