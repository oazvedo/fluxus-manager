using FluxusManager.API.Filters;
using FluxusManager.Application;
using FluxusManager.Infrastructure;
using FluxusManager.Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers(options =>
{
    options.Filters.Add<TenantFilter>();
    options.Filters.Add<ValidationFilter>();
    options.Filters.Add<ExceptionFilter>();
})
.ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = InvalidModelStateResponse.Create);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddApplicationModule();
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

app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();
