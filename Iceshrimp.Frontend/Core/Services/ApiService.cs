using Iceshrimp.Frontend.Core.ControllerModels;

namespace Iceshrimp.Frontend.Core.Services;

internal class ApiService(HttpClient client)
{
	internal readonly NoteControllerModel Notes = new(client);
}