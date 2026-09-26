using System.Reflection;
using System.Text.Json.Serialization;
using GraderApi.Calculator;
using GraderApi.ClassManagement;
using GraderApi.DemoData;
using GraderApi.ErrorHandling;
using GraderApi.Exams;
using GraderLogic.Calculator;
using GraderLogic.ClassManagement;
using GraderLogic.Data;
using GraderLogic.DemoData;
using GraderLogic.Exams;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
// Web defaults allow numbers as JSON strings, which makes OpenAPI describe every number as
// "integer | string" and the generated TypeScript client type them as "number | string".
//
// Missing properties of request records (e.g. a date) must not silently become default values. The OpenAPI spec
// marks constructor parameters as required anyway.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    options.SerializerOptions.RespectRequiredConstructorParameters = true;
});
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = ProblemDetailsCodes.AddMissingCode);
builder.Services.AddExceptionHandler<GraderLogicExceptionHandler>();
builder.Services.AddValidation();

// Connection string "Grader" (e.g. environment variable ConnectionStrings__Grader), default: data/grader.db in the
// repository root. Read when the first context is created, so tests can override it via configuration.
builder.Services.AddDbContext<GraderDbContext>((services, options) => options.UseSqlite(
    services.GetRequiredService<IConfiguration>().GetConnectionString("Grader") ?? GraderDatabase.GetDefaultConnectionString()));
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddSingleton<ICalculator, Calculator>();
builder.Services.AddScoped<IClassManagementService, ClassManagementService>();
builder.Services.AddScoped<IExamService, ExamService>();
builder.Services.AddScoped<IDemoDataService, DemoDataService>();

var app = builder.Build();

// Create or update the database schema on startup, but not during build-time OpenAPI generation (see api-client.md).
if (Assembly.GetEntryAssembly()?.GetName().Name != "GetDocument.Insider")
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<GraderDbContext>().Database.Migrate();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

var api = app.MapGroup("/api");
api.MapCalculatorEndpoints();
api.MapClassManagementEndpoints();
api.MapExamEndpoints();
api.MapDemoDataEndpoints();

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
