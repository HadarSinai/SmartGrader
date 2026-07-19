using FluentValidation;
using SmartGrader.Application.Common.HebrewDate;

namespace SmartGrader.Application.UseCases.LessonResults.ExportGradesPeriodReport;

public class ExportGradesPeriodReportValidator : AbstractValidator<ExportGradesPeriodReportQuery>
{
    public ExportGradesPeriodReportValidator()
    {
        RuleFor(x => x.FromHebrewYear).InclusiveBetween(5000, 6000).WithMessage("שנה עברית לא תקינה");
        RuleFor(x => x.FromHebrewMonth).InclusiveBetween(1, 13);
        RuleFor(x => x.FromHebrewDay).InclusiveBetween(1, 30);
        RuleFor(x => x)
            .Must(x => HebrewDateConverter.IsValidHebrewDate(x.FromHebrewYear, x.FromHebrewMonth, x.FromHebrewDay))
            .WithMessage("תאריך ההתחלה אינו קיים");

        RuleFor(x => x.ToHebrewYear).InclusiveBetween(5000, 6000).WithMessage("שנה עברית לא תקינה");
        RuleFor(x => x.ToHebrewMonth).InclusiveBetween(1, 13);
        RuleFor(x => x.ToHebrewDay).InclusiveBetween(1, 30);
        RuleFor(x => x)
            .Must(x => HebrewDateConverter.IsValidHebrewDate(x.ToHebrewYear, x.ToHebrewMonth, x.ToHebrewDay))
            .WithMessage("תאריך הסיום אינו קיים");
    }
}
