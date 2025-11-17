//using FluentValidation;
//using MediatR;
//using SmartGrader.Application.Common.Exceptions;

//namespace SmartGrader.Application.Common.Behaviors
//{
//    public class ValidationBehavior<TRequest, TResponse>
//        : IPipelineBehavior<TRequest, TResponse>
//        where TRequest : notnull
//    {
//        private readonly IEnumerable<IValidator<TRequest>> _validators;

//        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
//        {
//            _validators = validators;
//        }

//        public async Task<TResponse> Handle(
//            TRequest request,
//            RequestHandlerDelegate<TResponse> next,
//            CancellationToken cancellationToken)
//        {
//            if (!_validators.Any())
//                return await next();

//            var context = new ValidationContext<TRequest>(request);

//            var validationResults = await Task.WhenAll(
//                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

//            var failures = validationResults
//            .SelectMany(result => result.Errors)
//            .Where(error => error != null)
//            .ToList();

//            if (failures.Count != 0)
//                throw new ValidationException(failures);

//            return await next();
//        }
//    }
//}
using FluentValidation;
using MediatR;
using SmartGrader.Application.Common.Exceptions;

namespace SmartGrader.Application.Common.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (!_validators.Any())
                return await next();

            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(e => e != null)
                .ToList();

            if (failures.Count != 0)
                throw new AppValidationException(failures);   // ← פה חייב להיות AppValidationException

            return await next();
        }
    }
}
