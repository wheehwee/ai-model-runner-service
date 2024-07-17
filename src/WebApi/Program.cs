using System.Text.Json.Serialization;
using WebApi.Configuration;
using WebApi.Filters;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;


var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.All;
});

#if DEBUG
builder.Configuration.AddUserSecrets<Program>();
#endif
builder.Configuration.AddEnvironmentVariables();

builder.Host.ConfigureAppConfiguration((context, config) =>
{
    config.AddJsonFile("appsettings.secrets.json", true, true);
});

builder.Host.UseSerilog((ctx, lc) => { lc.ReadFrom.Configuration(ctx.Configuration); });

builder.Services
    .AddMvc(options => options.Filters.Add<ApiExceptionFilterAttribute>())
    .AddJsonOptions(options =>
    {
        //options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = ValidationErrorFilter.MakeValidationResponse;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var currentDirectory = Directory.GetCurrentDirectory();

var app = builder
    .ConfigureServices()
    .ConfigurePipeline();

using var scope = app.Services.CreateScope();
var logger = scope.ServiceProvider.GetService<ILogger<Program>>();
try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    logger?.LogError(ex.Message, ex);
}
finally
{
    //Cleanup
}