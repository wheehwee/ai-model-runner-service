using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Application.Models.Responses;

namespace WebApi.Filters
{
    public class ApiExceptionFilterAttribute : ExceptionFilterAttribute
    {
        private readonly IDictionary<Type, Action<ExceptionContext>> _exceptionHandlers;
        private readonly ILogger<ApiExceptionFilterAttribute> _logger;
        private readonly IHostEnvironment _hostingEnvironment;

        public ApiExceptionFilterAttribute(
            ILogger<ApiExceptionFilterAttribute> logger,
            IHostEnvironment hostingEnvironment
            )
        {
            // Register known exception types and handlers.
            _exceptionHandlers = new Dictionary<Type, Action<ExceptionContext>>
            {

            };

            _logger = logger;
            _hostingEnvironment = hostingEnvironment;
        }

        public override void OnException(ExceptionContext context)
        {
            HandleException(context);
            base.OnException(context);
        }

        private void HandleException(ExceptionContext context)
        {
            Type type = context.Exception.GetType();
            Action<ExceptionContext> handler = _exceptionHandlers
                    .Where(x => x.Key == type || x.Key == type.BaseType)
                    .Select(x => x.Value)
                    .FirstOrDefault();
            if (handler != null)
            {
                handler.Invoke(context);
                return;
            }

            HandleUnknownException(context);
        }

        private void HandleUnknownException(ExceptionContext context)
        {
            _logger.LogError(context.Exception, "Unhandled exception:\n{Exception}", context.Exception.Message);

            var error = new BaseApiResponse<string, string>
            {
                Data = string.Empty,
                Message = this.MakeInternalServerError(context),
                ErrorData = context.Exception.StackTrace,
            };

            context.Result = new ObjectResult(error) { StatusCode = StatusCodes.Status500InternalServerError };
            context.ExceptionHandled = true;
        }

        private string MakeInternalServerError(ExceptionContext context) =>
            $"{context.Exception.Message}. Internal exception message: {context.Exception.InnerException?.Message ?? "No inner exception"}";
    }
}
