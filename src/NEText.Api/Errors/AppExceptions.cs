namespace NEText.Api.Errors;

// Base class for "expected" errors that services throw on purpose (e.g. "username taken").
// GlobalExceptionHandler turns them into an HTTP response with the matching status code.
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

// 400: the request makes no sense (beyond simple field validation), e.g. sending a friend request to yourself
public class BadRequestException : AppException
{
    public BadRequestException(string message)
        : base(StatusCodes.Status400BadRequest, "Bad Request", message) { }
}

// 401: not logged in, or wrong credentials
public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message)
        : base(StatusCodes.Status401Unauthorized, "Unauthorized", message) { }
}

// 404: the requested thing doesn't exist
public class NotFoundException : AppException
{
    public NotFoundException(string message)
        : base(StatusCodes.Status404NotFound, "Not Found", message) { }
}

// 409: the request clashes with existing data (e.g. duplicate username)
public class ConflictException : AppException
{
    public ConflictException(string message)
        : base(StatusCodes.Status409Conflict, "Conflict", message) { }
}
