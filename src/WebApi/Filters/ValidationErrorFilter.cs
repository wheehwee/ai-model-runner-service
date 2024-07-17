using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using Application.Models.Responses;

namespace WebApi.Filters
{
    public static class ValidationErrorFilter
    {
        public static IActionResult MakeValidationResponse(ActionContext context)
        {
            var response = new BaseApiResponse<string, List<string>>()
            {
                Data = string.Empty,
                Message = "One or more validation errors have occured",
                ErrorData = new List<string>()
            };

            foreach (var keyModelStatePair in context.ModelState)
            {
                var errors = keyModelStatePair.Value.Errors;
                if (errors != null && errors.Count > 0)
                {
                    var errorMessages = new string[errors.Count];
                    for (var i = 0; i < errors.Count; i++)
                    {
                        errorMessages[i] = GetErrorMessage(errors[i]);
                    }
                    response.ErrorData.AddRange(errorMessages);
                }
            }

            var result = new BadRequestObjectResult(response);

            result.ContentTypes.Add("application/json");

            return result;
        }

        static string GetErrorMessage(ModelError error)
        {
            return string.IsNullOrEmpty(error.ErrorMessage) ?
            "The input was not valid." :
            error.ErrorMessage;
        }
    }
}
