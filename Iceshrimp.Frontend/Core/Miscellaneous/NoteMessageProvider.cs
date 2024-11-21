using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.Miscellaneous;

public abstract class NoteMessageProvider
{
	internal readonly Dictionary<string, EventHandler<NoteBase>> NoteChangedHandlers = new();

	public class NoteMessageHandler(EventHandler<NoteBase> handler, string id, NoteMessageProvider noteState)
		: IDisposable
	{
		public void Dispose()
		{
			noteState.Unregister(id, handler);
		}
	}

	private void Unregister(string id, EventHandler<NoteBase> func)
	{
		if (NoteChangedHandlers.ContainsKey(id))
		{
			#pragma warning disable CS8601
			NoteChangedHandlers[id] -= func;
			#pragma warning restore CS8601
		}
		else
		{
			throw new ArgumentException("Tried to unregister from callback that doesn't exist");
		}
	}

	public NoteMessageHandler Register(string id, EventHandler<NoteBase> func)
	{
		if (!NoteChangedHandlers.TryAdd(id, func))
		{
			NoteChangedHandlers[id] += func;
		}

		return new NoteMessageHandler(func, id, this);
	}
}