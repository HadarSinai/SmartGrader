using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Authorization;
using SmartGrader.Application.Dtos;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using System.Linq;

namespace SmartGrader.Application.UseCases.LessonResults.GetLessonResult;

public class GetLessonResultHandler
    : IRequestHandler<GetLessonResultQuery, LessonResultResponseDto?>
{
    private readonly ILessonResultRepository _lessonResultRepo;
    private readonly IAssignmentRepository _assignmentRepo;
    private readonly ISubmissionRepository _submissionRepo;
    private readonly ILessonRepository _lessonRepo;
    private readonly IMapper _mapper;

    public GetLessonResultHandler(
        ILessonResultRepository lessonResultRepo,
        IAssignmentRepository assignmentRepo,
        ISubmissionRepository submissionRepo,
        ILessonRepository lessonRepo,
        IMapper mapper)
    {
        _lessonResultRepo = lessonResultRepo;
        _assignmentRepo = assignmentRepo;
        _submissionRepo = submissionRepo;
        _lessonRepo = lessonRepo;
        _mapper = mapper;
    }

    public async Task<LessonResultResponseDto?> Handle(GetLessonResultQuery request, CancellationToken ct)
    {
        // בעלות המורה על השיעור — הועברה לכאן מהקונטרולר (אין לוגיקה עסקית בקונטרולרים).
        await LessonAccess.GetOwnedOrThrowAsync(_lessonRepo, request.LessonId, request.TeacherId, ct);

        var assignments = await _assignmentRepo.GetByLessonIdAsync(request.LessonId, ct);
        var total = assignments.Count;

        var submissions = await _submissionRepo.GetByStudentAndLessonAsync(request.StudentId, request.LessonId, ct);
        var completed = submissions.Where(s => s.Status == SubmissionStatus.Done).Select(s => s.AssignmentId).Distinct().Count();

        var existingResult = await _lessonResultRepo.GetAsync(request.StudentId, request.LessonId, ct);

        LessonResultResponseDto response;
        if (existingResult is not null)
        {
            response = _mapper.Map<LessonResultResponseDto>(existingResult);
        }
        else
        {
            response = new LessonResultResponseDto
            {
                Id = 0,
                StudentId = request.StudentId,
                LessonId = request.LessonId,
                IsComplete = false,
                FinalScore = null
            };
        }

        response.TotalAssignments = total;
        response.CompletedAssignments = completed;

        return response;
    }
}
