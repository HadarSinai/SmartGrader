using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGrader.Application.UseCases.LessonResults.CompleteLesson;

public record CompleteLessonCommand(int StudentId, int LessonId, double FinalScore);

