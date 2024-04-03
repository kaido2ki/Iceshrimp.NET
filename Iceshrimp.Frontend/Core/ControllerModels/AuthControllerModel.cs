using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class AuthControllerModel(ApiClient api)
{
	public Task<AuthResponse> GetAuthStatus() =>
		api.Call<AuthResponse>(HttpMethod.Get, "/auth");

	public Task<AuthResponse> Login(AuthRequest request) =>
		api.Call<AuthResponse>(HttpMethod.Post, "/auth", data: request);

	public Task<AuthResponse> Register(RegistrationRequest request) =>
		api.Call<AuthResponse>(HttpMethod.Put, "/auth", data: request);

	public Task<AuthResponse> ChangePassword(ChangePasswordRequest request) =>
		api.Call<AuthResponse>(HttpMethod.Patch, "/auth", data: request);
}