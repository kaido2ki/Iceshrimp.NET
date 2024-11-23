namespace Iceshrimp.Shared.Schemas.Web;

public class TwoFactorEnrollmentResponse
{
	public required string Secret { get; set; }
	public required string Url    { get; set; }
	public required string QrPng  { get; set; }
}