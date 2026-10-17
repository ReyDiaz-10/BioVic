using BioVic.Application.Exceptions;

namespace BioVic.Application.Services;

public static class PasswordPolicy
{
    public static void Validate(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 10)
            throw new ValidationException("La contraseña debe tener al menos 10 caracteres.");
        if (!password.Any(char.IsUpper))
            throw new ValidationException("La contraseña debe incluir una letra mayúscula.");
        if (!password.Any(char.IsLower))
            throw new ValidationException("La contraseña debe incluir una letra minúscula.");
        if (!password.Any(char.IsDigit))
            throw new ValidationException("La contraseña debe incluir un número.");
        if (!password.Any(character => !char.IsLetterOrDigit(character)))
            throw new ValidationException("La contraseña debe incluir un símbolo.");
    }
}
