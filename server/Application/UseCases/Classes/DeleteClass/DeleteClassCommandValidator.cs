using FluentValidation;

namespace SmartGrader.Application.UseCases.Classes.DeleteClass
{
    public class DeleteClassCommandValidator : AbstractValidator<DeleteClassCommand>
    {
        public DeleteClassCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Id must be greater than 0.");
        }
    }
}
