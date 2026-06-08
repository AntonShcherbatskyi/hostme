namespace HostMe.Domain.Services.Models;

public class ApiResponse
{
    public List<string> Errors { get; init; } = new();
    public bool IsError { get; init; }

    public static ApiResponse Failure(List<string> errors) => new() { Errors = errors, IsError = true };
    public static ApiResponse Failure(string error) => new() { Errors = [error], IsError = true };
    public static ApiResponse Ok() => new() { IsError = false };
}

public class ApiResponse<T>
{
    public T? Data { get; init; }
    public List<string> Errors { get; init; } = new();
    public bool IsError { get; init; }

    public static ApiResponse<T> Success(T data) => new() { Data = data, IsError = false };
    public static ApiResponse<T> Failure(List<string> errors) => new() { Errors = errors, IsError = true };
    public static ApiResponse<T> Failure(string error) => new() { Errors = [error], IsError = true };
}