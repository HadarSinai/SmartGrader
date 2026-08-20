using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Authorization;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Assignments;
using SmartGrader.Application.Dtos.Lessons;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Assignments.UpdateAssignment
{
    public class UpdateAssignmentHandler
        : IRequestHandler<UpdateAssignmentCommand, AssignmentResponseDto>
    {
        private readonly IAssignmentRepository _repository;
        private readonly ILessonRepository _lessonRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UpdateAssignmentHandler(
            IAssignmentRepository repository,
            ILessonRepository lessonRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _lessonRepository = lessonRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<AssignmentResponseDto> Handle(
            UpdateAssignmentCommand request,
            CancellationToken cancellationToken)
        {
            await LessonAccess.GetOwnedOrThrowAsync(_lessonRepository, request.LessonId, request.TeacherId, cancellationToken);

            var assignment = await _repository.GetByIdAsync(request.AssignmentId, cancellationToken);

            if (assignment is null || assignment.LessonId != request.LessonId)
                throw new NotFoundException(nameof(Assignment), request.AssignmentId);

            assignment.Title = request.Dto.Title;
            assignment.Description = request.Dto.Description;
            assignment.IsBonus = request.Dto.IsBonus;
            assignment.BonusValue = request.Dto.BonusValue;
            if (request.Dto.MethodName is not null)
                assignment.MethodName = request.Dto.MethodName;
            // תקף כי ה-Validator בודק IsEnumName לפני שהמטפל רץ
            assignment.GradingMode = Enum.Parse<GradingMode>(request.Dto.GradingMode, true);

            assignment.SetTests(_mapper.Map<List<TestCase>>(request.Dto.Tests ?? new()));
            assignment.SetExpectedFiles(_mapper.Map<List<ExpectedFile>>(request.Dto.ExpectedFiles ?? new()));
            assignment.SetReferenceSolution(
                _mapper.Map<List<ReferenceSolutionFile>>(request.Dto.ReferenceSolution ?? new()));
            assignment.SetStructuralRules(
                _mapper.Map<List<StructuralRule>>(request.Dto.StructuralRules ?? new()));

            assignment.TestsAllocation = request.Dto.TestsAllocation;
            assignment.RetryThreshold = request.Dto.RetryThreshold;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<AssignmentResponseDto>(assignment);
        }
    }
}
