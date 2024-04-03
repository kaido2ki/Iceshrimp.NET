using Iceshrimp.Frontend.Core.ControllerModels;

namespace Iceshrimp.Frontend.Core.Services;

internal class ApiService(ApiClient client)
{
	public readonly NoteControllerModel         Notes         = new(client);
	public readonly UserControllerModel         Users         = new(client);
	public readonly AuthControllerModel         Auth          = new(client);
	public readonly AdminControllerModel        Admin         = new(client);
	public readonly TimelineControllerModel     Timelines     = new(client);
	public readonly NotificationControllerModel Notifications = new(client);
	
	public void SetBearerToken(string token) => client.SetToken(token);
}