using FluxusManager.API.Filters;
using FluxusManager.API.Logging;
using FluxusManager.Application;
using FluxusManager.Application.Options;
using FluxusManager.Infrastructure;
using FluxusManager.Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);

builder.AddStructuredLogging();

builder.Services.AddControllers(options =>
{
    options.Filters.Add<TenantFilter>();
    options.Filters.Add<AuditFilter>();
    options.Filters.Add<ValidationFilter>();
    options.Filters.Add<ExceptionFilter>();
})
.ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = InvalidModelStateResponse.Create);
// Todo ProblemDetails leva o correlation id, que o cliente informa ao suporte para achar os logs.
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["correlationId"] = CorrelationIdMiddleware.Get(context.HttpContext));
builder.Services.AddOpenApi();

builder.Services.AddApplicationModule();
builder.Services.Configure<FilialOptions>(builder.Configuration.GetSection(FilialOptions.SectionName));
builder.Services.AddInfraModule(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("postgres");

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
    await app.Services.MigrateDatabaseAsync();

// No servidor a API fica atrás do nginx em /api (PathBase=/api); localmente não há prefixo.
var pathBase = app.Configuration["PathBase"];
if (!string.IsNullOrEmpty(pathBase))
    app.UsePathBase(pathBase);

app.UseRequestLogging();

// Erros fora dos controllers (middlewares, rotas inexistentes) também saem como ProblemDetails.
app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseRouting();

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    // Relativo à página do Swagger, para funcionar com ou sem o PathBase.
    options.SwaggerEndpoint("../openapi/v1.json", "FluxusManager API v1");
    options.DocumentTitle = "FluxusManager API";
});

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseUserLogContext();

app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();
