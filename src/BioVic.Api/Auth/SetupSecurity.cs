using System.Security.Cryptography;
using System.Text;
using BioVic.Application.Exceptions;
using BioVic.Infrastructure.Options;

namespace BioVic.Api.Auth;

public static class SetupSecurity
{
    public static void EnsureAllowed(HttpContext httpContext, IHostEnvironment environment, BioVicOptions options)
    {
        if (!environment.IsProduction()) return;
        if (!options.SetupEnabled)
            throw new ForbiddenException("La creación inicial del Administrador Maestro está deshabilitada en producción.");

        var suppliedToken = httpContext.Request.Headers["X-BioVic-Setup-Token"].ToString();
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(options.SetupToken));
        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(suppliedToken));
        if (!CryptographicOperations.FixedTimeEquals(expectedHash, suppliedHash))
            throw new AuthenticationException("El token de configuración inicial no es válido.");
    }
}
