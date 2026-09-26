using System.Text.Json.Serialization;
using GraderApi.Calculator;
using GraderApi.ErrorHandling;
using GraderLogic.Calculator;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
// Web defaults allow numbers as JSON strings, which makes OpenAPI describe every number as
// "integer | string" and the generated TypeScript client type them as "number | string".
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GraderLogicExceptionHandler>();

builder.Services.AddSingleton<ICalculator, Calculator>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

var api = app.MapGroup("/api");
api.MapCalculatorEndpoints();

// Unknown API routes must return 404 instead of falling through to the SPA's index.html.
api.MapFallback(() => TypedResults.NotFound()).ExcludeFromDescription();

// Outside of Development, the backend serves the Angular app (built into wwwroot) incl. client-side routes.
// In Development, the Angular dev server serves the app and proxies /api to this backend.
if (!app.Environment.IsDevelopment())
{
    app.MapStaticAssets();
    app.MapFallbackToFile("index.html");
}

app.Run();
