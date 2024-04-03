using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class NotificationControllerModel(ApiClient api)
{
	[LinkPagination(20, 80)]
	public Task<List<NotificationResponse>> GetNotifications(PaginationQuery pq) =>
		api.Call<List<NotificationResponse>>(HttpMethod.Get, "/notifications", pq);

	public Task MarkNotificationAsRead(string id) =>
		api.Call(HttpMethod.Post, $"/notifications/{id}/read");

	public Task MarkAllNotificationsAsRead() =>
		api.Call(HttpMethod.Post, "/notifications/read");

	public Task DeleteNotification(string id) =>
		api.Call(HttpMethod.Delete, $"/notifications/{id}");

	public Task DeleteAllNotifications() =>
		api.Call(HttpMethod.Delete, "/notifications");
}