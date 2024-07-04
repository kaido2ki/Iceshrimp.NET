using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.Miscellaneous;

internal class ApiException(ErrorResponse error) : Exception
{
	public ErrorResponse Response => error;
}