namespace Bloqqer.Api.Middleware;

public class BloqqerExceptionHandler(ILogger<BloqqerExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var errorId = Guid.NewGuid().ToString();

        ProblemDetails problemDetails = exception switch
        {
            BloqqerUnauthorizedException => exception.ToProblemDetails(HttpStatusCode.Unauthorized, "Unauthorized"),
            BloqqerValidationException => exception.ToProblemDetails(HttpStatusCode.BadRequest, "Bad request"),
            BloqqerNotFoundException => exception.ToProblemDetails(HttpStatusCode.NotFound, "Not Found"),
            _ => exception.ToProblemDetails(HttpStatusCode.InternalServerError, "An unexpected error occurred",
                $"{exception.Message}. This error has been logged with an error id of {errorId}")
        };

        logger.LogError(exception, "An error occurred at {time} UTC. Error id = {errorId}", utcNow, errorId);

        httpContext.Response.StatusCode = problemDetails.Status ?? 500;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}

public static class BloqqerExceptionHandlingExtensions
{
    public static ProblemDetails ToProblemDetails(
        this Exception e, 
        HttpStatusCode 
        statusCode, string title, 
        string? details = null) => new()
    {
        Status = (int)statusCode,
        Title = title,
        Type = e.GetType().Name,
        Detail = details ?? e.Message
    };
}