using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json.Serialization;
using ChatGptNet;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.OpenApi.Models;
using MinimalHelpers.OpenApi;
using MinimalHelpers.Routing;
using OperationResults.AspNetCore.Http;
using SimpleAuthentication;
using SimpleAuthentication.ApiKey;
using TinyHelpers.AspNetCore.Extensions;
using TinyHelpers.AspNetCore.Swagger;
using TinyHelpers.Extensions;
using TranslatorGpt.Authentication;
using TranslatorGpt.BusinessLayer.Services;
using TranslatorGpt.BusinessLayer.Services.Interfaces;
using TranslatorGpt.BusinessLayer.Settings;
using TranslatorGpt.BusinessLayer.Validations;
using TranslatorGpt.Extensions;
using TranslatorGpt.Swagger;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true);

// Add services to the container.
var settings = builder.Services.ConfigureAndGet<AppSettings>(builder.Configuration, nameof(AppSettings));
var swagger = builder.Services.ConfigureAndGet<SwaggerSettings>(builder.Configuration, nameof(SwaggerSettings));

builder.Services.AddHttpContextAccessor();
builder.Services.AddRazorPages();

builder.Services.AddRequestLocalization(settings.SupportedCultures);

builder.Services.AddWebOptimizer(minifyCss: true, minifyJavaScript: builder.Environment.IsProduction());

builder.Services.AddChatGpt((services, options) =>
{
    options.UseConfiguration(builder.Configuration);

    var httpContext = services.GetRequiredService<IHttpContextAccessor>().HttpContext;
    var request = httpContext?.Request;
    var user = httpContext?.User;

    // Searches the claims for the API Key and uses it for querying ChatGPT.
    var apiKey = user?.FindFirstValue("ApiKey").GetValueOrDefault(string.Empty);

    var provider = request?.Headers["x-provider"].ToString().ToLowerInvariant().GetValueOrDefault().GetValueOrDefault(string.Empty);
    var resourceName = request?.Headers["x-resource-name"].ToString().GetValueOrDefault(string.Empty);
    var model = request?.Headers["x-model"].ToString().GetValueOrDefault();

    // Checks if we want to use OpenAI or Azure OpenAI service.
    if (provider == "azure")
    {
        options.UseAzure(resourceName, apiKey);
    }
    else
    {
        options.UseOpenAI(apiKey);
    }

    options.DefaultModel = model;
});

builder.Services.AddValidatorsFromAssemblyContaining<TranslationRequestValidator>();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        var statusCode = context.ProblemDetails.Status.GetValueOrDefault(StatusCodes.Status500InternalServerError);
        context.ProblemDetails.Type ??= $"https://httpstatuses.io/{statusCode}";
        context.ProblemDetails.Title ??= ReasonPhrases.GetReasonPhrase(statusCode);
        context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
        context.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
    };
});

builder.Services.Scan(scan => scan
          .FromAssemblyOf<ITranslatorService>()
            .AddClasses(classes => classes.InNamespaceOf<TranslatorService>().Where(type => type.Name.EndsWith("Service")))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

builder.Services.AddSimpleAuthentication(builder.Configuration);
builder.Services.AddTransient<IApiKeyValidator, ApiKeyValidator>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

if (swagger.IsEnabled)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "TranslatorGPT API", Version = "v1" });

        options.AddDefaultResponse();
        options.AddSimpleAuthentication(builder.Configuration);
        options.AddMissingSchemas();
    });
}

builder.Services.AddOperationResult(options =>
{
    options.ErrorResponseFormat = ErrorResponseFormat.List;
});

//builder.Services.AddRateLimiter(options =>
//{
//    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
//    {
//        return RateLimitPartition.GetTokenBucketLimiter("Default", _ => new TokenBucketRateLimiterOptions
//        {
//            TokenLimit = 100,
//            TokensPerPeriod = 50,
//            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
//            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
//        });
//    });

//    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
//    options.OnRejected = (context, token) =>
//    {
//        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var window))
//        {
//            context.HttpContext.Response.Headers.RetryAfter = window.TotalSeconds.ToString();
//        }

//        return ValueTask.CompletedTask;
//    };
//});

var app = builder.Build();
app.Services.GetRequiredService<IWebHostEnvironment>().ApplicationName = settings.ApplicationName;

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.UseWhen(context => context.IsWebRequest(), builder =>
{
    if (!app.Environment.IsDevelopment())
    {
        builder.UseExceptionHandler("/errors/500");

        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        builder.UseHsts();
    }

    builder.UseStatusCodePagesWithReExecute("/errors/{0}");
});

app.UseWhen(context => context.IsApiRequest(), builder =>
{
    if (!app.Environment.IsDevelopment())
    {
        // Error handling
        builder.UseExceptionHandler(new ExceptionHandlerOptions
        {
            AllowStatusCode404Response = true,
            ExceptionHandler = async (HttpContext context) =>
            {
                var problemDetailsService = context.RequestServices.GetService<IProblemDetailsService>();
                var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
                var error = exceptionHandlerFeature?.Error;

                // Write as JSON problem details
                await problemDetailsService.WriteAsync(new()
                {
                    HttpContext = context,
                    AdditionalMetadata = exceptionHandlerFeature?.Endpoint?.Metadata,
                    ProblemDetails =
                    {
                        Status = context.Response.StatusCode,
                        Title = error?.GetType().FullName ?? "An error occurred while processing your request",
                        Detail = error?.Message
                    }
                });
            }
        });
    }

    builder.UseStatusCodePages();
});

app.UseWebOptimizer();
app.UseStaticFiles();

if (swagger.IsEnabled)
{
    app.UseMiddleware<SwaggerBasicAuthenticationMiddleware>();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TranslatorGPT API v1");
        options.InjectStylesheet("/css/swagger.css");
    });
}

app.UseRouting();
app.UseRequestLocalization();

app.UseWhen(context => context.IsApiRequest(), builder =>
{
    builder.UseAuthentication();
    builder.UseAuthorization();

    //builder.UseRateLimiter();
});

// app.UseCors();

// In Razor Pages apps and apps with controllers, UseOutputCache must be called after UseRouting.
//app.UseOutputCache();

app.MapEndpoints();
app.MapRazorPages();

app.Run();