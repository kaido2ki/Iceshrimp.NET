// using Iceshrimp.Shared.Schemas.Web;
//
// namespace Iceshrimp.Frontend.Core.Services;
//
// internal class MessageService
// {
// 	public event EventHandler<NoteResponse>? AnyNoteChanged;
// 	public event EventHandler<NoteResponse>? AnyNoteDeleted;
//
// 	private readonly Dictionary<(string, Type), EventHandler<NoteResponse>> _noteChangedHandlers = new();
//
// 	public enum Type
// 	{
// 		Updated,
// 		Deleted
// 	}
//
// 	// public NoteMessageHandler Register(string id, EventHandler<NoteResponse> func, Type type)
// 	// {
// 	// 	var tuple = (id, type);
// 	// 	if (_noteChangedHandlers.ContainsKey(tuple))
// 	// 	{
// 	// 		_noteChangedHandlers[tuple] += func;
// 	// 	}
// 	// 	else
// 	// 	{
// 	// 		_noteChangedHandlers.Add(tuple, func);
// 	// 	}
// 	//
// 	// 	return new NoteMessageHandler(func, id, type, this);
// 	// }
//
// 	// private void Unregister(string id, EventHandler<NoteResponse> func, Type type)
// 	// {
// 	// 	var tuple = (id, type);
// 	// 	if (_noteChangedHandlers.ContainsKey(tuple))
// 	// 	{
// 	// 		#pragma warning disable CS8601
// 	// 		_noteChangedHandlers[tuple] -= func;
// 	// 		#pragma warning restore CS8601
// 	// 	}
// 	// 	else
// 	// 	{
// 	// 		throw new ArgumentException("Tried to unregister from callback that doesn't exist");
// 	// 	}
// 	// }
//
// 	// public class NoteMessageHandler : IDisposable
// 	// {
// 	// 	private readonly EventHandler<NoteResponse> _handler;
// 	// 	private readonly string                     _id;
// 	// 	private readonly Type                       _type;
// 	// 	private readonly MessageService             _messageService;
// 	//
// 	// 	public NoteMessageHandler(
// 	// 		EventHandler<NoteResponse> handler, string id, Type type, MessageService messageService
// 	// 	)
// 	// 	{
// 	// 		_handler        = handler;
// 	// 		_id             = id;
// 	// 		_type           = type;
// 	// 		_messageService = messageService;
// 	// 	}
// 	//
// 	// 	public void Dispose()
// 	// 	{
// 	// 		_messageService.Unregister(_id, _handler, _type);
// 	// 	}
// 	// }
//
// 	public Task UpdateNoteAsync(NoteResponse note)
// 	{
// 		AnyNoteChanged?.Invoke(this, note);
// 		_noteChangedHandlers.TryGetValue((note.Id, Type.Updated), out var xHandler);
// 		xHandler?.Invoke(this, note);
// 		return Task.CompletedTask;
// 	}
//
// 	public Task DeleteNoteAsync(NoteResponse note)
// 	{
// 		AnyNoteDeleted?.Invoke(this, note);
// 		_noteChangedHandlers.TryGetValue((note.Id, Type.Deleted), out var xHandler);
// 		xHandler?.Invoke(this, note);
// 		return Task.CompletedTask;
// 	}
// }