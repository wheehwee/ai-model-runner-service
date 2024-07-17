using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebApi.Filters.Swagger
{
    public class AuthorizationOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Get Authorize attribute
            var attributes = context.MethodInfo.DeclaringType.GetCustomAttributes(true)
                                    .Union(context.MethodInfo.GetCustomAttributes(true))
                                    .OfType<AuthorizeAttribute>();

            if (attributes != null && attributes.Count() > 0)
            {
                var attr = attributes.ToList()[0];

                // Add what should be show inside the security section
                IList<string> securityInfos = new List<string>
                {
                    $"{nameof(AuthorizeAttribute.Policy)}:{attr.Policy}",
                    $"{nameof(AuthorizeAttribute.Roles)}:{attr.Roles}",
                    $"{nameof(AuthorizeAttribute.AuthenticationSchemes)}:{attr.AuthenticationSchemes}"
                };

                switch (attr.AuthenticationSchemes)
                {
                    case var p when p == JwtBearerDefaults.AuthenticationScheme:
                    default:
                        operation.Security = new List<OpenApiSecurityRequirement>()
                    {
                        new OpenApiSecurityRequirement()
                        {
                            {
                                new OpenApiSecurityScheme
                                {
                                    Reference = new OpenApiReference
                                    {
                                        Id = "Bearer", // Must fit the defined Id of SecurityDefinition in global configuration
                                        Type = ReferenceType.SecurityScheme
                                    }
                                },
                                securityInfos
                            }
                        }
                    };
                        break;
                }
            }
            else
            {
                operation.Security.Clear();
            }
        }
    }
}
