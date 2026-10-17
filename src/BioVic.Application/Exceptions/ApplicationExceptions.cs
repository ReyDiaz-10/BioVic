namespace BioVic.Application.Exceptions;

public class BioVicException(string message) : Exception(message) { }

public sealed class ValidationException(string message) : BioVicException(message) { }

public sealed class NotFoundException(string message) : BioVicException(message) { }

public sealed class ConflictException(string message) : BioVicException(message) { }

public sealed class AuthenticationException(string message) : BioVicException(message) { }

public sealed class ForbiddenException(string message) : BioVicException(message) { }
