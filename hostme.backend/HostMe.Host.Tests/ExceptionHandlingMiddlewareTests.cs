using System.Net;
using System.Text.Json;
using HostMe.Domain.Constants;
using HostMe.Domain.Services.Models;
using HostMe.Host.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace HostMe.Tests;

public class ExceptionHandlingMiddlewareTests
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddlewareTests()
    {
        _logger = Substitute.For<ILogger<ExceptionHandlingMiddleware>>();
    }

    [Fact]
    public async Task InvokeAsync_WhenNoException_CallsNextMiddleware()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ExceptionHandlingMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var responseStream = new MemoryStream();
        context.Response.Body = responseStream;

        RequestDelegate next = (ctx) => throw new ArgumentException("Invalid argument specified.");
        var middleware = new ExceptionHandlingMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        responseStream.Position = 0;
        using var reader = new StreamReader(responseStream);
        var responseBody = await reader.ReadToEndAsync();
        
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(responseBody, jsonOptions);

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.IsError);
        Assert.Single(apiResponse.Errors);
        Assert.Equal("Invalid argument specified.", apiResponse.Errors[0]);
        Assert.Null(apiResponse.Data);
    }

    [Fact]
    public async Task InvokeAsync_WhenInvalidOperationException_ReturnsConflict()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var responseStream = new MemoryStream();
        context.Response.Body = responseStream;

        RequestDelegate next = (ctx) => throw new InvalidOperationException("Operation not valid.");
        var middleware = new ExceptionHandlingMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.Conflict, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        responseStream.Position = 0;
        using var reader = new StreamReader(responseStream);
        var responseBody = await reader.ReadToEndAsync();
        
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(responseBody, jsonOptions);

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.IsError);
        Assert.Single(apiResponse.Errors);
        Assert.Equal("Operation not valid.", apiResponse.Errors[0]);
        Assert.Null(apiResponse.Data);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnexpectedException_ReturnsInternalServerError()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var responseStream = new MemoryStream();
        context.Response.Body = responseStream;

        RequestDelegate next = (ctx) => throw new Exception("Database connection failed.");
        var middleware = new ExceptionHandlingMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        responseStream.Position = 0;
        using var reader = new StreamReader(responseStream);
        var responseBody = await reader.ReadToEndAsync();
        
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(responseBody, jsonOptions);

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.IsError);
        Assert.Single(apiResponse.Errors);
        Assert.Equal(ErrorMessages.General.UnexpectedError + "Database connection failed.", apiResponse.Errors[0]);
        Assert.Null(apiResponse.Data);
    }
}
