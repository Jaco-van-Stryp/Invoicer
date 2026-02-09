using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Invoicer.Infrastructure.Validation
{
    public class ValidationBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken
        )
        {
            var context = new ValidationContext(request);
            var results = new List<ValidationResult>();

            if (!Validator.TryValidateObject(request, context, results, validateAllProperties: true))
            {
                var errors = string.Join("; ", results.Select(r => r.ErrorMessage));
                throw new ValidationException(errors);
            }

            return await next(cancellationToken);
        }
    }
}
