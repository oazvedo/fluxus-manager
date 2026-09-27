using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using System.Collections.Generic;

namespace FluxusManager.API.Security;

public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var components = document.Components ?? new OpenApiComponents();
        document.Components = components;
        components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Informe o access token JWT."
        };
        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
        if (document.Paths.TryGetValue("/auth/login", out var loginPath) && loginPath.Operations is { } operations)
        {
            var loginOperation = operations.FirstOrDefault(operation => operation.Key.ToString() == "Post").Value;
            if (loginOperation is not null)
                loginOperation.Security = [];
        }
        return Task.CompletedTask;
    }
}
