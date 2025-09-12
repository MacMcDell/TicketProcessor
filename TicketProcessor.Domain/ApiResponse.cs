namespace TicketProcessor.Domain.Response;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
    public string? Message { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null)
        => new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message, params string[] errors)
        => new() { Success = false, Message = message, Errors = errors?.ToList() ?? new() };

    public static ApiResponse<T> Fail(string message, IEnumerable<string> errors)
        => new() { Success = false, Message = message, Errors = errors?.ToList() ?? new() };
}