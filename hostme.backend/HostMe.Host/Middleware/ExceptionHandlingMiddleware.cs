using System.Net;
using System.Text.Json;
using HostMe.Domain.Constants;
using HostMe.Domain.Services.Models;

namespace HostMe.Host.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        HttpStatusCode statusCode;
        var errors = new List<string>();

        switch (exception)
        {
            case ArgumentException argEx:
                statusCode = HttpStatusCode.BadRequest;
                errors.Add(argEx.Message);
                break;
            case InvalidOperationException invEx:
                statusCode = HttpStatusCode.Conflict;
                errors.Add(invEx.Message);
                break;
            default:
                statusCode = HttpStatusCode.InternalServerError;
                errors.Add(ErrorMessages.General.UnexpectedError + exception.Message);
                break;
        }

        context.Response.StatusCode = (int)statusCode;

        var apiResponse = ApiResponse<object>.Failure(errors);
        var jsonOptions = new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        };
        var responseJson = JsonSerializer.Serialize(apiResponse, jsonOptions);

        await context.Response.WriteAsync(responseJson);
    }
}
