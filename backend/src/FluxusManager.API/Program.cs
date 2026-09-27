using System.Text;
using FluxusManager.API.Filters;
using FluxusManager.API.Logging;
using FluxusManager.API.Security;
using FluxusManager.Application;
using FluxusManager.Application.Options;
using FluxusManager.Application.Security;
using FluxusManager.Infrastructure;
using FluxusManager.Infrastructure.Database;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

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
builder.Services.AddOpenApi(options => options
    .AddDocumentTransformer<BearerSecuritySchemeTransformer>()
    .AddOperationTransformer<BearerSecuritySchemeTransformer>());

builder.Services.AddApplicationModule(builder.Configuration);
builder.Services.Configure<FilialOptions>(builder.Configuration.GetSection(FilialOptions.SectionName));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (Encoding.UTF8.GetByteCount(jwt.SigningKey) < 32)
    throw new InvalidOperationException("Jwt:SigningKey deve conter ao menos 32 bytes e ser configurada por secret em produção.");
if (string.IsNullOrWhiteSpace(jwt.Issuer) || string.IsNullOrWhiteSpace(jwt.Audience) || jwt.AccessTokenMinutes is < 1 or > 60)
    throw new InvalidOperationException("Jwt:Issuer e Jwt:Audience são obrigatórios; Jwt:AccessTokenMinutes deve estar entre 1 e 60.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwt.Issuer,
        ValidateAudience = true,
        ValidAudience = jwt.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = "sub",
        RoleClaimType = "role"
    };
});
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    foreach (var permission in PermissionCatalog.All)
        options.AddPolicy(permission, policy => policy.RequireAuthenticatedUser().RequireClaim("permissions", permission));
});
builder.Services.AddInfraModule(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("postgres");

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
    await app.Services.MigrateDatabaseAsync();

await app.Services.SeedDevelopmentAsync();

// No servidor a API fica atrás do nginx em /api (PathBase=/api); localmente não há prefixo.
var pathBase = app.Configuration["PathBase"];
if (!string.IsNullOrEmpty(pathBase))
    app.UsePathBase(pathBase);

app.UseRequestLogging();

// Erros fora dos controllers (middlewares, rotas inexistentes) também saem como ProblemDetails.
app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseRouting();

app.MapOpenApi().AllowAnonymous();
app.UseSwaggerUI(options =>
{
    // Relativo à página do Swagger, para funcionar com ou sem o PathBase.
    options.SwaggerEndpoint("../openapi/v1.json", "FluxusManager API v1");
    options.DocumentTitle = "FluxusManager API";
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseUserLogContext();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

await app.RunAsync();
