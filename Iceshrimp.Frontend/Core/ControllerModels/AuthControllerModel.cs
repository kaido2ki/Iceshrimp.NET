using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class AuthControllerModel(ApiClient api)
{
	public Task<AuthResponse> GetAuthStatusAsync() =>
		api.CallAsync<AuthResponse>(HttpMethod.Get, "/auth");

	public Task<AuthResponse> LoginAsync(AuthRequest request) =>
		api.CallAsync<AuthResponse>(HttpMethod.Post, "/auth/login", data: request);

	public Task<AuthResponse> RegisterAsync(RegistrationRequest request) =>
		api.CallAsync<AuthResponse>(HttpMethod.Post, "/auth/register", data: request);

	public Task<AuthResponse> ChangePasswordAsync(ChangePasswordRequest request) =>
		api.CallAsync<AuthResponse>(HttpMethod.Post, "/auth/change-password", data: request);
}