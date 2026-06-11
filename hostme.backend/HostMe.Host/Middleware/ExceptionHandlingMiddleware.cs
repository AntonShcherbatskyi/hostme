using System.Net;
using System.Net.Mime;
using System.Text.Json;
using HostMe.Domain.Constants;
using HostMe.Domain.Services.Models;

namespace HostMe.Host.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogMessages.Http.UnhandledException);
            await HandleExceptionAsync(context, ex, exposeDetails: _environment.IsDevelopment());
        }
    }

    private static async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        bool exposeDetails)
    {
        context.Response.ContentType = MediaTypeNames.Application.Json;

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

            case KeyNotFoundException knfEx:
                statusCode = HttpStatusCode.NotFound;
                errors.Add(knfEx.Message);
                break;

            case UnauthorizedAccessException unAuthEx:
                statusCode = HttpStatusCode.Forbidden;
                errors.Add(unAuthEx.Message);
                break;

            default:
                statusCode = HttpStatusCode.InternalServerError;
                errors.Add(exposeDetails
                    ? ErrorMessages.General.UnexpectedErrorWithDetail + exception.Message
                    : ErrorMessages.General.UnexpectedError);
                break;
        }

        context.Response.StatusCode = (int)statusCode;

        var apiResponse = ApiResponse<object>.Failure(errors);
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        await context.Response.WriteAsync(JsonSerializer.Serialize(apiResponse, jsonOptions));
    }
}
