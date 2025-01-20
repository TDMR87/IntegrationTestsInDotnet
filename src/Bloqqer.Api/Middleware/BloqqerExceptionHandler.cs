namespace Bloqqer.Api.Middleware;

public class BloqqerExceptionHandler(ILogger<BloqqerExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var errorId = Guid.NewGuid().ToString();

        ProblemDetails problemDetails = exception switch
        {
            BloqqerUnauthorizedException validationException => new ProblemDetails
            {
                Status = (int)HttpStatusCode.Unauthorized,
                Type = validationException.GetType().Name,
                Title = "Unauthorized",
                Detail = validationException.Message
            },

            BloqqerValidationException validationException => new ProblemDetails
            {
                Status = (int)HttpStatusCode.BadRequest,
                Type = validationException.GetType().Name,
                Title = "Bad Request",
                Detail = validationException.Message,
            },

            BloqqerNotFoundException notFoundException => new ProblemDetails
            {
                Status = (int)HttpStatusCode.NotFound,
                Type = notFoundException.GetType().Name,
                Title = "Not Found",
                Detail = notFoundException.Message
            },

            _ => new ProblemDetails
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Type = exception.GetType().Name,
                Title = $"An unexpected error occurred",
                Detail = $"{exception.Message}. This error has been logged with an error id of {errorId}"
            }
        };

        logger.LogError("An error occurred at {time} UTC. Error id = {errorId}. {error}",
            utcNow,
            errorId,
            exception.ToString());

        httpContext.Response.StatusCode = problemDetails.Status ?? 500;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}  