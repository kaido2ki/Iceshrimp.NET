using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.Services.NoteStore;

public class StateSynchronizer
{
	public event EventHandler<NoteBase>? NoteChanged;
	public event EventHandler<NoteBase>? NoteDeleted;

	public void Broadcast(NoteBase note)
	{
		NoteChanged?.Invoke(this, note);
	}

	public void Delete(NoteBase note)
	{
		NoteDeleted?.Invoke(this, note);
	}
}
