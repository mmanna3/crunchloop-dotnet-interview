using TodoApi.Application.Exceptions;

namespace TodoApi.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            await HandleAppException(context, ex);
        }
        catch (Exception ex)
        {
            await HandleUnknownException(context, ex);
        }
    }

    private static Task HandleAppException(HttpContext context, AppException ex)
    {
        var statusCode = ex switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            ValidationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError,
        };

        var response = new { error = ex.Code, message = ex.Message };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        return context.Response.WriteAsJsonAsync(response);
    }

    private static Task HandleUnknownException(HttpContext context, Exception ex)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var response = new { error = "internal_error", message = ex.Message };

        return context.Response.WriteAsJsonAsync(response);
    }
}
