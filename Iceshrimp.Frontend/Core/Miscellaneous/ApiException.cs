using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.Miscellaneous;

public class ApiException : Exception
{
    public ErrorResponse? Response { get; private init; }

    public ApiException(string? message) : base(message) { }

    public ApiException(string? message, Exception? innerException) : base(message, innerException) { }

    public ApiException(ErrorResponse error)
    {
        Response = error;
    }
}
