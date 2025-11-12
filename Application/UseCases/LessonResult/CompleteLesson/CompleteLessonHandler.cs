using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGrader.Application.UseCases.LessonResults.CompleteLesson;

public class CompleteLessonHandler
{
    private readonly ILessonResultRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteLessonHandler(ILessonResultRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(CompleteLessonCommand command, CancellationToken ct = default)
    {
        // 1️⃣ לבדוק אם כבר קיים רשומה לסטודנט/שיעור
        var result = await _repository.GetAsync(command.StudentId, command.LessonId, ct)
                     ?? LessonResult.Create(command.StudentId, command.LessonId);

        // 2️⃣ להפעיל לוגיקה עסקית מתוך הדומיין (ככה שומרים על החוקיות)
        result.CompleteWith(command.FinalScore);

        // 3️⃣ אם חדש – להוסיף ל־Repository
        if (result.Id == 0)
            await _repository.AddAsync(result, ct);

        // 4️⃣ לשמור שינויים
        await _unitOfWork.SaveChangesAsync(ct);
    }
}

