using Microsoft.AspNetCore.Http;

namespace ECommerce.ServiceDefaults;

public sealed record OperationResult<T>(
    int StatusCode,
    T? Value = default,
    string? Location = null,
    string? Error = null,
    IDictionary<string, string[]>? ValidationErrors = null)
{
    public static OperationResult<T> Ok(T value) => new(StatusCodes.Status200OK, value);

    public static OperationResult<T> Created(string location, T value) => new(StatusCodes.Status201Created, value, location);

    public static OperationResult<T> Accepted(string location, T value) => new(StatusCodes.Status202Accepted, value, location);

    public static OperationResult<T> Conflict(string error) => new(StatusCodes.Status409Conflict, Error: error);

    public static OperationResult<T> NotFound(string error = "Resource was not found.") => new(StatusCodes.Status404NotFound, Error: error);

    public static OperationResult<T> Validation(IDictionary<string, string[]> errors) =>
        new(StatusCodes.Status400BadRequest, ValidationErrors: errors);

    public IResult ToHttpResult()
    {
        if (ValidationErrors is not null)
        {
            return Results.ValidationProblem(ValidationErrors);
        }

        if (Error is not null)
        {
            return StatusCode switch
            {
                StatusCodes.Status404NotFound => Results.NotFound(new { error = Error }),
                StatusCodes.Status409Conflict => Results.Conflict(new { error = Error }),
                _ => Results.Problem(Error, statusCode: StatusCode),
            };
        }

        return StatusCode switch
        {
            StatusCodes.Status201Created => Results.Created(Location ?? string.Empty, Value),
            StatusCodes.Status202Accepted => Results.Accepted(Location ?? string.Empty, Value),
            StatusCodes.Status200OK => Results.Ok(Value),
            _ => Results.Json(Value, statusCode: StatusCode),
        };
    }
}
