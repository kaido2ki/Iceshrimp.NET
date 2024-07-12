using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class NotificationControllerModel(ApiClient api)
{
	[LinkPagination(20, 80)]
	public Task<List<NotificationResponse>> GetNotifications(PaginationQuery pq) =>
		api.Call<List<NotificationResponse>>(HttpMethod.Get, "/notifications", pq);

	public Task<bool> MarkNotificationAsRead(string id) =>
		api.CallNullable(HttpMethod.Post, $"/notifications/{id}/read");

	public Task MarkAllNotificationsAsRead() =>
		api.Call(HttpMethod.Post, "/notifications/read");

	public Task<bool> DeleteNotification(string id) =>
		api.CallNullable(HttpMethod.Delete, $"/notifications/{id}");

	public Task DeleteAllNotifications() =>
		api.Call(HttpMethod.Delete, "/notifications");
}