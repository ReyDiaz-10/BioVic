using BioVic.Domain.Enums;

namespace BioVic.Application.DTOs;

public sealed record CreateMasterRequest(
    string FirstName,
    string LastName,
    string Username,
    string Password,
    string ConfirmPassword,
    string? Email);

public sealed record LoginRequest(string Username, string Password, string? ComputerName);

public sealed record MasterProfileDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Username,
    string? Email,
    DateTimeOffset? PasswordChangedAtUtc);

public sealed record UpdateMasterProfileRequest(
    string FirstName,
    string LastName,
    string Username,
    string? Email,
    string CurrentPassword,
    string? NewPassword,
    string? ConfirmNewPassword);

public sealed record AuthSessionDto(
    string Token,
    Guid UserId,
    Guid SessionId,
    string DisplayName,
    string Username,
    UserRole Role,
    DateTimeOffset ExpiresAtUtc);

public sealed record SessionPrincipalDto(
    Guid UserId,
    Guid SessionId,
    string Username,
    UserRole Role,
    DateTimeOffset ExpiresAtUtc);

public sealed record CreateUserRequest(
    string FirstName,
    string LastName,
    string Username,
    string Password,
    string? Email,
    UserRole Role,
    bool HasAllLocations,
    IReadOnlyList<Guid> LocationIds);

public sealed record UserSummaryDto(
    Guid Id,
    string DisplayName,
    string Username,
    string? Email,
    UserRole Role,
    bool IsEnabled,
    bool HasAllLocations,
    IReadOnlyList<Guid> LocationIds,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastLoginAtUtc);

public sealed record UpdateUserAccessRequest(
    UserRole Role,
    bool IsEnabled,
    bool HasAllLocations,
    IReadOnlyList<Guid> LocationIds);

public sealed record DeleteUserRequest(string Reason);
