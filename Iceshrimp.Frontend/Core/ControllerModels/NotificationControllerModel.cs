using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class NotificationControllerModel(ApiClient api)
{
	[LinkPagination(20, 80)]
	public Task<List<NotificationResponse>> GetNotificationsAsync(PaginationQuery pq) =>
		api.CallAsync<List<NotificationResponse>>(HttpMethod.Get, "/notifications", pq);

	public Task<bool> MarkNotificationAsReadAsync(string id) =>
		api.CallNullableAsync(HttpMethod.Post, $"/notifications/{id}/read");

	public Task MarkAllNotificationsAsReadAsync() =>
		api.CallAsync(HttpMethod.Post, "/notifications/read");

	public Task<bool> DeleteNotificationAsync(string id) =>
		api.CallNullableAsync(HttpMethod.Delete, $"/notifications/{id}");

	public Task DeleteAllNotificationsAsync() =>
		api.CallAsync(HttpMethod.Delete, "/notifications");
}