using FluxusManager.API.Filters;
using FluxusManager.Application;
using FluxusManager.Infrastructure;
using FluxusManager.Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers(options =>
{
    options.Filters.Add<TenantFilter>();
    options.Filters.Add<ExceptionFilter>();
});
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddApplicationModule();
builder.Services.AddInfraModule(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("postgres");

var app = builder.Build();

// Erros fora dos controllers (middlewares, rotas inexistentes) também saem como ProblemDetails.
app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "FluxusManager API v1");
    options.DocumentTitle = "FluxusManager API";
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
