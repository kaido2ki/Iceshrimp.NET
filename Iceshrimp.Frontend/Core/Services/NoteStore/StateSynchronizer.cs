using System.Diagnostics;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.Services.NoteStore;

internal class StateSynchronizer: IAsyncDisposable
{
	private readonly StreamingService    _streamingService;
	public event EventHandler<NoteBase>? NoteChanged;
	public event EventHandler<NoteBase>? NoteDeleted;

	public StateSynchronizer(StreamingService streamingService)
	{
		_streamingService       =  streamingService;
		_streamingService.NoteUpdated += NoteUpdated;
	}
	
	public void Broadcast(NoteBase note)
	{
		// Trace Logging for broadcast note is null;
		if (note == null) throw new UnreachableException("Note null when not nullable");
		NoteChanged?.Invoke(this, note);
	}

	public void Delete(NoteBase note)
	{
		NoteDeleted?.Invoke(this, note);
	}

	private void NoteUpdated(object? sender, NoteResponse noteResponse)
	{
		NoteChanged?.Invoke(this, noteResponse);
	}

	public async ValueTask DisposeAsync()
	{
		_streamingService.NoteUpdated -= NoteUpdated;
		await _streamingService.DisposeAsync();
	}
}
