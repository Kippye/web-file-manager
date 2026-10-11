namespace Application.Contracts;

public enum EApplicationErrorCode
{
    // Cryptography errors
    CryptographyError,
    AuthenticationTagError,

    // IO errors
    IOError,
    FilePermissionError,
    DatabaseError,

    // Validation errors
    ValidationError,
    RequiredFieldMissing,
    InvalidFormat,
    ValueOutOfRange,

    // Business logic errors
    BusinessRuleViolation,
    InvalidOperation,
    ResourceNotFound,
    ResourceAlreadyExists,

    // Authorization errors
    UnauthorizedAccess,
    ForbiddenOperation,

    // User errors
    UserNotFound,
    UserAlreadyExists,
    InvalidCredentials,

    // Parsing errors
    ParsingError,

    // General errors
    UnknownError
}

