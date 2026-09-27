using Microsoft.AspNetCore.Authorization;

namespace FluxusManager.API.Security;

public sealed class HasPermissionAttribute(string permission) : AuthorizeAttribute(permission);
