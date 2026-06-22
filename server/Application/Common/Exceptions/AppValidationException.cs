
namespace SmartGrader.Application.Common.Exceptions
{
    public class AppValidationException : Exception
    {
        public IDictionary<string, string[]> Errors { get; }

        public AppValidationException()
            : base("One or more validation failures have occurred.")
        {
            Errors = new Dictionary<string, string[]>();
        }

        public AppValidationException(IEnumerable<FluentValidation.Results.ValidationFailure> failures)
    : this()
        {
            Errors = failures
                .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
                .ToDictionary(fg => fg.Key, fg => fg.ToArray());
        }
    }
}
