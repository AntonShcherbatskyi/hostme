namespace HostMe.Domain.Services.Models;

public class ApiResponse<T>
{
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool IsError { get; set; }

    public static ApiResponse<T> Success(T data) => new() { Data = data, IsError = false };
    public static ApiResponse<T> Failure(List<string> errors) => new() { Errors = errors, IsError = true };
    public static ApiResponse<T> Failure(string error) => new() { Errors = new List<string> { error }, IsError = true };
}
