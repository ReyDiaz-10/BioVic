using System.Security.Cryptography;
using System.Text;
using BioVic.Application.Contracts;
using BioVic.Application.DTOs;
using BioVic.Application.Exceptions;
using BioVic.Application.Services;
using BioVic.Domain.Entities;
using BioVic.Domain.Enums;
using BioVic.Infrastructure.Data;
using BioVic.Infrastructure.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BioVic.Infrastructure.Services;

public sealed class AuthService(
    BioVicDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    IAuditService auditService,
    BioVicOptions options) : IAuthService
{
    public async Task<SetupStatusDto> GetSetupStatusAsync(CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.Users.IgnoreQueryFilters()
            .AnyAsync(user => user.Role == UserRole.MasterAdministrator, cancellationToken);
        return new(!exists);
    }

    public async Task<AuthSessionDto> CreateMasterAsync(CreateMasterRequest request, RequestContext context, CancellationToken cancellationToken = default)
    {
        ValidateIdentity(request.FirstName, request.LastName, request.Username);
        if (request.Password != request.ConfirmPassword)
            throw new ValidationException("La confirmación de la contraseña no coincide.");
        PasswordPolicy.Validate(request.Password);

        if (await dbContext.Users.IgnoreQueryFilters().AnyAsync(user => user.Role == UserRole.MasterAdministrator, cancellationToken))
            throw new ConflictException("BioVic ya tiene un Administrador Maestro configurado.");

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Username = request.Username.Trim(),
            NormalizedUsername = NormalizeUsername(request.Username),
            Email = EmptyToNull(request.Email),
            Role = UserRole.MasterAdministrator,
            IsEnabled = true,
            HasAllLocations = true,
            PasswordChangedAtUtc = DateTimeOffset.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        dbContext.Users.Add(user);

        var session = CreateSession(user, context);
        var actor = context with
        {
            UserId = user.Id,
            Username = user.Username,
            Role = user.Role,
            SessionId = session.SessionId.ToString("D")
        };
        await auditService.AppendAsync(new(actor, "MASTER_CREATED", "User", user.Id.ToString("D"), null,
            "Se completó la configuración inicial y se creó el Administrador Maestro."), cancellationToken);
        return session;
    }

    public async Task<AuthSessionDto> LoginAsync(LoginRequest request, RequestContext context, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeUsername(request.Username);
        var user = await dbContext.Users.SingleOrDefaultAsync(candidate => candidate.NormalizedUsername == normalized, cancellationToken);
        if (user is null || !user.IsEnabled)
        {
            await auditService.AppendAsync(new(context, "LOGIN_FAILED", "User", null, null,
                "Intento de inicio de sesión fallido para un usuario inexistente o deshabilitado."), cancellationToken);
            throw new AuthenticationException("Usuario o contraseña incorrectos.");
        }

        var now = DateTimeOffset.UtcNow;
        if (user.LockoutEndUtc is not null && user.LockoutEndUtc > now)
            throw new AuthenticationException($"La cuenta está bloqueada temporalmente hasta {user.LockoutEndUtc:HH:mm} UTC.");

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= options.FailedLoginLimit)
            {
                user.LockoutEndUtc = now.AddMinutes(options.LockoutMinutes);
                user.FailedLoginAttempts = 0;
            }
            await auditService.AppendAsync(new(context, "LOGIN_FAILED", "User", user.Id.ToString("D"), null,
                "Intento de inicio de sesión fallido."), cancellationToken);
            throw new AuthenticationException("Usuario o contraseña incorrectos.");
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        user.FailedLoginAttempts = 0;
        user.LockoutEndUtc = null;
        user.LastLoginAtUtc = now;

        var session = CreateSession(user, context with { ComputerName = request.ComputerName ?? context.ComputerName });
        var actor = context with
        {
            UserId = user.Id,
            Username = user.Username,
            Role = user.Role,
            SessionId = session.SessionId.ToString("D")
        };
        await auditService.AppendAsync(new(actor, "LOGIN_SUCCEEDED", "User", user.Id.ToString("D"), null,
            "Inicio de sesión correcto."), cancellationToken);
        return session;
    }

    public async Task<MasterProfileDto> GetMasterProfileAsync(RequestContext context, CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentMasterAsync(context, cancellationToken);
        return MapMasterProfile(user);
    }

    public async Task<MasterProfileDto> UpdateMasterProfileAsync(
        UpdateMasterProfileRequest request,
        RequestContext context,
        CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentMasterAsync(context, cancellationToken);
        ValidateIdentity(request.FirstName, request.LastName, request.Username);

        if (string.IsNullOrEmpty(request.CurrentPassword))
            throw new ValidationException("La contraseña actual es obligatoria para guardar los cambios.");

        var currentPasswordVerification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (currentPasswordVerification == PasswordVerificationResult.Failed)
            throw new AuthenticationException("La contraseña actual no es correcta.");

        var normalizedUsername = NormalizeUsername(request.Username);
        if (await dbContext.Users.IgnoreQueryFilters().AnyAsync(
                candidate => candidate.Id != user.Id && candidate.NormalizedUsername == normalizedUsername,
                cancellationToken))
            throw new ConflictException($"Ya existe un usuario llamado {request.Username.Trim()}.");

        var changesPassword = !string.IsNullOrEmpty(request.NewPassword)
            || !string.IsNullOrEmpty(request.ConfirmNewPassword);
        if (changesPassword)
        {
            if (request.NewPassword != request.ConfirmNewPassword)
                throw new ValidationException("La confirmación de la nueva contraseña no coincide.");
            PasswordPolicy.Validate(request.NewPassword!);
        }

        var previous = new
        {
            user.FirstName,
            user.LastName,
            user.Username,
            user.Email
        };
        var previousUsername = user.Username;

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Username = request.Username.Trim();
        user.NormalizedUsername = normalizedUsername;
        user.Email = EmptyToNull(request.Email);
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;
        user.UpdatedByUserId = user.Id;

        if (changesPassword)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword!);
            user.PasswordChangedAtUtc = DateTimeOffset.UtcNow;
            user.FailedLoginAttempts = 0;
            user.LockoutEndUtc = null;

            if (Guid.TryParse(context.SessionId, out var currentSessionId))
            {
                var otherSessions = await dbContext.Sessions
                    .Where(session => session.UserId == user.Id && session.Id != currentSessionId && session.RevokedAtUtc == null)
                    .ToListAsync(cancellationToken);
                foreach (var session in otherSessions)
                    session.RevokedAtUtc = DateTimeOffset.UtcNow;
            }
        }
        else if (currentPasswordVerification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.CurrentPassword);
        }

        var updatedActor = context with { Username = user.Username };
        await auditService.AppendAsync(new(updatedActor, "MASTER_PROFILE_UPDATED", "User", user.Id.ToString("D"), null,
            $"El Administrador Maestro actualizó su cuenta interna de BioVic ({previousUsername} → {user.Username}).",
            System.Text.Json.JsonSerializer.Serialize(previous),
            System.Text.Json.JsonSerializer.Serialize(new
            {
                user.FirstName,
                user.LastName,
                user.Username,
                user.Email,
                PasswordChanged = changesPassword
            })), cancellationToken);

        return MapMasterProfile(user);
    }

    public async Task LogoutAsync(string rawToken, RequestContext context, CancellationToken cancellationToken = default)
    {
        var hash = HashToken(rawToken);
        var session = await dbContext.Sessions.SingleOrDefaultAsync(candidate => candidate.TokenHash == hash, cancellationToken);
        if (session is not null && session.RevokedAtUtc is null)
        {
            session.RevokedAtUtc = DateTimeOffset.UtcNow;
            await auditService.AppendAsync(new(context, "LOGOUT", "Session", session.Id.ToString("D"), null,
                "Cierre de sesión."), cancellationToken);
        }
    }

    public async Task<SessionPrincipalDto?> ValidateSessionAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken)) return null;
        var hash = HashToken(rawToken);
        var now = DateTimeOffset.UtcNow;
        return await dbContext.Sessions.AsNoTracking()
            .Where(session => session.TokenHash == hash && session.RevokedAtUtc == null && session.ExpiresAtUtc > now && session.User.IsEnabled)
            .Select(session => new SessionPrincipalDto(
                session.UserId, session.Id, session.User.Username, session.User.Role, session.ExpiresAtUtc))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private AuthSessionDto CreateSession(User user, RequestContext context)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var session = new Session
        {
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(options.SessionHours),
            ComputerName = context.ComputerName,
            IpAddress = context.IpAddress
        };
        dbContext.Sessions.Add(session);
        return new(rawToken, user.Id, session.Id, $"{user.FirstName} {user.LastName}".Trim(), user.Username, user.Role, session.ExpiresAtUtc);
    }

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static MasterProfileDto MapMasterProfile(User user) => new(
        user.Id, user.FirstName, user.LastName, user.Username, user.Email, user.PasswordChangedAtUtc);

    private async Task<User> GetCurrentMasterAsync(RequestContext context, CancellationToken cancellationToken)
    {
        if (context.UserId is null || context.Role != UserRole.MasterAdministrator)
            throw new ForbiddenException("Esta acción requiere una sesión de Administrador Maestro.");

        return await dbContext.Users.SingleOrDefaultAsync(
                user => user.Id == context.UserId.Value && user.Role == UserRole.MasterAdministrator,
                cancellationToken)
            ?? throw new AuthenticationException("La cuenta del Administrador Maestro no está disponible.");
    }

    private static string NormalizeUsername(string value) => value.Trim().ToUpperInvariant();
    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidateIdentity(string firstName, string lastName, string username)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new ValidationException("El nombre es obligatorio.");
        if (string.IsNullOrWhiteSpace(lastName)) throw new ValidationException("Los apellidos son obligatorios.");
        if (string.IsNullOrWhiteSpace(username) || username.Trim().Length < 4)
            throw new ValidationException("El usuario debe tener al menos 4 caracteres.");
    }
}
