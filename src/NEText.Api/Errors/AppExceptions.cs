namespace NEText.Api.Errors;

public abstract class AppException : Exception
{
    public int StatusCode { get; }
    public string Title { get; }

    protected AppException(int statusCode, string title, string message) : base(message)
    {
        StatusCode = statusCode;
        Title = title;
    }
}

public class BadRequestException : AppException
{
    public BadRequestException(string message)
        : base(StatusCodes.Status400BadRequest, "Bad Request", message) { }
}

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message)
        : base(StatusCodes.Status401Unauthorized, "Unauthorized", message) { }
}

public class ForbiddenException : AppException
{
    public ForbiddenException(string message)
        : base(StatusCodes.Status403Forbidden, "Forbidden", message) { }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message)
        : base(StatusCodes.Status404NotFound, "Not Found", message) { }
}

public class ConflictException : AppException
{
    public ConflictException(string message)
        : base(StatusCodes.Status409Conflict, "Conflict", message) { }
}
