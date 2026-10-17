using BioVic.Application.Exceptions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;

namespace BioVic.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var (status, title, detail) = exception switch
            {
                ValidationException => (StatusCodes.Status400BadRequest, "Datos inválidos", exception.Message),
                AuthenticationException => (StatusCodes.Status401Unauthorized, "No fue posible iniciar sesión", exception.Message),
                ForbiddenException => (StatusCodes.Status403Forbidden, "Acceso denegado", exception.Message),
                NotFoundException => (StatusCodes.Status404NotFound, "No encontrado", exception.Message),
                ConflictException => (StatusCodes.Status409Conflict, "Conflicto", exception.Message),
                AntiforgeryValidationException => (StatusCodes.Status400BadRequest, "Solicitud no válida", "El formulario expiró o no pudo verificarse. Vuelva a abrir el enlace QR."),
                BadHttpRequestException => (StatusCodes.Status400BadRequest, "Solicitud no válida", "La solicitud no tiene un formato permitido."),
                DbUpdateException => (StatusCodes.Status409Conflict, "No fue posible guardar", "Los datos entran en conflicto con un registro existente."),
                _ => (StatusCodes.Status500InternalServerError, "Error interno", "BioVic no pudo completar la operación. Revise el registro técnico.")
            };

            if (status >= 500) logger.LogError(exception, "Unhandled BioVic API exception for {Method} {Path}.", context.Request.Method, context.Request.Path);
            else logger.LogWarning("Handled request error {Status}: {Message}", status, exception.Message);
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new { type = "about:blank", title, status, detail }, context.RequestAborted);
        }
    }
}
