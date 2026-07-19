using MediatR;

namespace SmartGrader.Application.UseCases.Classes.FinishYear
{
    // מארכב את כל הכיתות הפעילות; מחזיר את מספר הכיתות שאורכבו
    public record FinishYearCommand() : IRequest<int>;
}
