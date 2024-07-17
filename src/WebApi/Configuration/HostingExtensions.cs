using Application.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.System.Text.Json;
using WebApi.Filters.Swagger;
using Refit;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.Extensions.FileProviders;
using Common.Extensions;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using FluentValidation;
using Application.ImageGeneration.Interfaces;
using Application.ImageGeneration;
using StableDiffusion.ML.OnnxRuntime.Configuration;
using Application.Interfaces.TextToImageRuns;
using Infrastructure.Data.Services.DataServices;

namespace WebApi.Configuration
{
    public static partial class HostingExtensions
    {
        public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
        {
            builder.AddRedis();
            builder.AddSwaggerConfig();
            builder.AddCommon();
            builder.AddAppApiVersioning();
            builder.AddInfrastructureServiceBus();
            builder.Services.AddDirectoryBrowser();

            return builder.Build();
        }

        public static void AddAppApiVersioning(this WebApplicationBuilder builder)
        {
            builder.Services.AddApiVersioning(opt =>
            {
                opt.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
                opt.AssumeDefaultVersionWhenUnspecified = true;
                opt.ReportApiVersions = true;
                opt.ApiVersionReader = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader(),
                    new HeaderApiVersionReader("x-api-version"),
                    new MediaTypeApiVersionReader("x-api-version"));
            });

            builder.Services.AddVersionedApiExplorer(
                options =>
                {
                    options.GroupNameFormat = "'v'VVV";
                    options.SubstituteApiVersionInUrl = true;
                });
        }

        private static void AddRedis(this WebApplicationBuilder builder)
        {
            var _redisOptions = builder.Configuration.GetSection(AppConstants.RedisSection).Get<RedisConfiguration>();
            builder.Services.AddStackExchangeRedisExtensions<SystemTextJsonSerializer>(_redisOptions);
        }

        static void AddSwaggerConfig(this WebApplicationBuilder builder)
        {
            builder.Services.AddSwaggerGen(opt =>
            {
                opt.OperationFilter<AuthorizationOperationFilter>();

                opt.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
                opt.IgnoreObsoleteActions();
                opt.IgnoreObsoleteProperties();
                opt.CustomSchemaIds(type => type.FullName);

                opt.SwaggerDoc("v1", new OpenApiInfo { Title = "ai.modelrunner", Version = "v1" });
                opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "bearer"
                });
            });
        }

        public static void AddCommon(this WebApplicationBuilder builder)
        {
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddHttpClient();

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters.ValidTypes = new[] { "at+jwt" };
                    options.TokenValidationParameters.ValidateAudience = false;
                    options.Events = new JwtBearerEvents()
                    {
                        OnAuthenticationFailed = context =>
                        {
                            Console.WriteLine(context.Exception.Message);
                            Console.WriteLine(context.Exception.StackTrace);
                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddAuthorization(options =>
            {
                var defaultAuthorizationPolicy =
                    new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme, "Bearer")
                        .RequireAuthenticatedUser();
                options.DefaultPolicy = defaultAuthorizationPolicy.Build();

                options.AddPolicy(AppConstants.PolicyWrite, builder =>
                {
                    builder.Combine(defaultAuthorizationPolicy.Build());
                    builder.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                    builder.RequireClaim(JwtClaimTypes.Scope, AppConstants.ScopeAdmin, AppConstants.ScopeWrite);
                });

                options.AddPolicy(AppConstants.PolicyRead, builder =>
                {
                    builder.Combine(defaultAuthorizationPolicy.Build());
                    builder.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                    builder.RequireClaim(JwtClaimTypes.Scope, AppConstants.ScopeAdmin, AppConstants.ScopeWrite,
                        AppConstants.ScopeRead);
                });

                options.AddPolicy(AppConstants.PolicyAdmin, builder =>
                {
                    builder.Combine(defaultAuthorizationPolicy.Build());
                    builder.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                    builder.RequireClaim(JwtClaimTypes.Scope, AppConstants.ScopeAdmin);
                });
            });

            //builder.Services.AddAutoMapper(cfg => { cfg.AddProfile(new ProspectMappingProfile()); });

            //builder.Services.AddValidatorsFromAssemblyContaining<SaveProspectRequestValidator>();

            builder.Services.AddScoped<ITextToImageRunDataService, TextToImageRunDataService>();

            builder.Services.AddStableDiffusion();
            builder.Services.AddSingleton<ITextToImageGenerator, TextToImageGenerator>();
        }

        public static WebApplication ConfigurePipeline(this WebApplication app)
        {
            app.Use((context, next) =>
            {
                context.Request.EnableBuffering();
                return next();
            });
            app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            app.UseForwardedHeaders();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.Map("/",
                    async () =>
                    {
                        return
                            $"AI Model runner. DateTime: {TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, "Asia/Bangkok".ToTimeZoneInfo())}";
                    });
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
            });
            app.MapControllers();

            //Configure the HTTP request pipeline.
            app.UseHttpsRedirection();

            app.UseSwagger(o => { });
            app.UseSwaggerUI((c) => { c.SwaggerEndpoint($"v1/swagger.json", "AI Model runner v1"); });
           
            return app;
        }
    }
}