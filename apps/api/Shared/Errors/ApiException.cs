namespace LineCom.Api.Shared.Errors;

public sealed class ApiException : Exception
{
    public ApiException(string code, string message, int statusCode)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }

    public string Code { get; }

    public int StatusCode { get; }
}
