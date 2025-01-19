using Iceshrimp.Shared.Helpers;

namespace Iceshrimp.Frontend.Core.Miscellaneous;

public interface IStreamingItemProvider<T> where T : IIdentifiable
{
	event EventHandler<T> ItemPublished;
}
