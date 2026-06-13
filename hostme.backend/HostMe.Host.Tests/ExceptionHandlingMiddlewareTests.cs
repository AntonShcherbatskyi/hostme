using System.Net;
using System.Text.Json;
using HostMe.Domain.Constants;
using HostMe.Domain.Services.Models;
using HostMe.Host.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace HostMe.Tests;

public class ExceptionHandlingMiddlewareTests
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _developmentEnvironment;
    private readonly IHostEnvironment _productionEnvironment;

    public ExceptionHandlingMiddlewareTests()
    {
        _logger = Substitute.For<ILogger<ExceptionHandlingMiddleware>>();

        _developmentEnvironment = Substitute.For<IHostEnvironment>();
        _developmentEnvironment.EnvironmentName.Returns(Environments.Development);

        _productionEnvironment = Substitute.For<IHostEnvironment>();
        _productionEnvironment.EnvironmentName.Returns(Environments.Production);
    }

    private ExceptionHandlingMiddleware Create(RequestDelegate next, bool isDevelopment = true) =>
        new(next, _logger, isDevelopment ? _developmentEnvironment : _productionEnvironment);

    [Fact]
    public async Task InvokeAsync_WhenNoException_CallsNextMiddleware()
    {
        var context = new DefaultHttpContext();
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        await Create(next).InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenArgumentException_ReturnsBadRequest()
    {
        var context = new DefaultHttpContext();
        var responseStream = new MemoryStream();
        context.Response.Body = responseStream;

        RequestDelegate next = (ctx) => throw new ArgumentException("Invalid argument specified.");

        await Create(next).InvokeAsync(context);

        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        var apiResponse = await DeserializeResponse<ApiResponse<object>>(responseStream);

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.IsError);
        Assert.Single(apiResponse.Errors);
        Assert.Equal("Invalid argument specified.", apiResponse.Errors[0]);
        Assert.Null(apiResponse.Data);
    }

    [Fact]
    public async Task InvokeAsync_WhenInvalidOperationException_ReturnsConflict()
    {
        var context = new DefaultHttpContext();
        var responseStream = new MemoryStream();
        context.Response.Body = responseStream;

        RequestDelegate next = (ctx) => throw new InvalidOperationException("Operation not valid.");

        await Create(next).InvokeAsync(context);

        Assert.Equal((int)HttpStatusCode.Conflict, context.Response.StatusCode);

        var apiResponse = await DeserializeResponse<ApiResponse<object>>(responseStream);

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.IsError);
        Assert.Single(apiResponse.Errors);
        Assert.Equal("Operation not valid.", apiResponse.Errors[0]);
        Assert.Null(apiResponse.Data);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnexpectedException_InDevelopment_ExposesDetail()
    {
        var context = new DefaultHttpContext();
        var responseStream = new MemoryStream();
        context.Response.Body = responseStream;

        RequestDelegate next = (ctx) => throw new Exception("Database connection failed.");

        await Create(next, isDevelopment: true).InvokeAsync(context);

        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);

        var apiResponse = await DeserializeResponse<ApiResponse<object>>(responseStream);

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.IsError);
        Assert.Single(apiResponse.Errors);
        Assert.Equal(
            ErrorMessages.General.UnexpectedErrorWithDetail + "Database connection failed.",
            apiResponse.Errors[0]);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnexpectedException_InProduction_HidesDetail()
    {
        var context = new DefaultHttpContext();
        var responseStream = new MemoryStream();
        context.Response.Body = responseStream;

        RequestDelegate next = (ctx) => throw new Exception("Database connection failed.");

        await Create(next, isDevelopment: false).InvokeAsync(context);

        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);

        var apiResponse = await DeserializeResponse<ApiResponse<object>>(responseStream);

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.IsError);
        Assert.Single(apiResponse.Errors);
        Assert.Equal(ErrorMessages.General.UnexpectedError, apiResponse.Errors[0]);
        Assert.DoesNotContain("Database connection failed.", apiResponse.Errors[0]);
    }

    [Fact]
    public async Task InvokeAsync_WhenKeyNotFoundException_ReturnsNotFound()
    {
        var context = new DefaultHttpContext();
        var responseStream = new MemoryStream();
        context.Response.Body = responseStream;

        RequestDelegate next = (ctx) => throw new KeyNotFoundException("Resource not found.");

        await Create(next).InvokeAsync(context);

        Assert.Equal((int)HttpStatusCode.NotFound, context.Response.StatusCode);

        var apiResponse = await DeserializeResponse<ApiResponse<object>>(responseStream);

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.IsError);
        Assert.Single(apiResponse.Errors);
        Assert.Equal("Resource not found.", apiResponse.Errors[0]);
        Assert.Null(apiResponse.Data);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnauthorizedAccessException_ReturnsForbidden()
    {
        var context = new DefaultHttpContext();
        var responseStream = new MemoryStream();
        context.Response.Body = responseStream;

        RequestDelegate next = (ctx) => throw new UnauthorizedAccessException("Access denied.");

        await Create(next).InvokeAsync(context);

        Assert.Equal((int)HttpStatusCode.Forbidden, context.Response.StatusCode);

        var apiResponse = await DeserializeResponse<ApiResponse<object>>(responseStream);

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.IsError);
        Assert.Single(apiResponse.Errors);
        Assert.Equal("Access denied.", apiResponse.Errors[0]);
        Assert.Null(apiResponse.Data);
    }

    private static async Task<T?> DeserializeResponse<T>(MemoryStream stream)
    {
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var body = await reader.ReadToEndAsync();
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        return JsonSerializer.Deserialize<T>(body, options);
    }
}
