using FluentValidation;

namespace SmartGrader.Application.UseCases.Students.GetStudents
{
    public class GetStudentsQueryValidator : AbstractValidator<GetStudentsQuery>
    {
        public GetStudentsQueryValidator()
        {
            // כרגע אין שדות לבדוק,
            // אבל אפשר להוסיף חוקים בעתיד (למשל paging וכו')
        }
    }
}
