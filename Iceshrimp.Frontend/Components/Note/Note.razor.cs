using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Iceshrimp.Frontend.Components.Note;

public partial class Note : IDisposable
{
	[Inject] private ApiService                                  ApiService        { get; set; } = null!;
	[Inject] private NavigationManager                           NavigationManager { get; set; } = null!;
	[Inject] private ComposeService                              ComposeService    { get; set; } = null!;
	[Inject] private MessageService                              MessageSvc        { get; set; } = null!;
	[Inject] private IStringLocalizer<Localization.Localization> Loc               { get; set; } = null!;

	[Parameter] [EditorRequired] public required NoteResponse NoteResponse { get; set; }
	[Parameter]                  public          bool         Indented     { get; set; }
	private                                      bool         _shouldRender       = false;
	private                                      IDisposable  _noteChangedHandler = null!;
	private                                      bool         _overrideHide       = false;

	public void React(EmojiResponse emoji)
	{
		var target                             = NoteResponse.Renote ?? NoteResponse;
		var x                                  = target.Reactions.FirstOrDefault(p => p.Name == emoji.Name);
		if (x is null || x.Reacted == false) _ = AddReact(emoji.Name, emoji.Sensitive, emoji.PublicUrl);
	}

	public async Task AddReact(string name, bool sensitive, string? url = null)
	{
		var target = NoteResponse.Renote ?? NoteResponse;
		var x      = target.Reactions.FirstOrDefault(p => p.Name == name);
		if (x == null)
		{
			target.Reactions.Add(new NoteReactionSchema
			{
				NoteId    = target.Id,
				Name      = name,
				Count     = 1,
				Reacted   = true,
				Url       = url,
				Sensitive = sensitive
			});
		}
		else x.Count++;

		Broadcast();
		try
		{
			await ApiService.Notes.ReactToNoteAsync(target.Id, name);
		}
		catch (ApiException)
		{
			if (x!.Count > 1) x.Count--;
			else target.Reactions.Remove(x);
			Broadcast();
		}
	}

	public async Task RemoveReact(string name)
	{
		var target   = NoteResponse.Renote ?? NoteResponse;
		var rollback = target.Reactions.First(p => p.Name == name);
		if (rollback.Count > 1) rollback.Count--;
		else target.Reactions.Remove(rollback);
		Broadcast();
		try
		{
			await ApiService.Notes.RemoveReactionFromNoteAsync(target.Id, name);
		}
		catch (ApiException)
		{
			if (rollback.Count >= 1) rollback.Count++;
			else target.Reactions.Add(rollback);
			Broadcast();
		}
	}

	private void Broadcast()
	{
		MessageSvc.UpdateNoteAsync(NoteResponse);
	}

	public async Task ToggleLike()
	{
		var target = NoteResponse.Renote ?? NoteResponse;
		if (target.Liked)
		{
			try
			{
				target.Liked = false;
				target.Likes--;
				Broadcast();
				await ApiService.Notes.UnlikeNoteAsync(target.Id);
			}
			catch (ApiException)
			{
				target.Liked = true;
				target.Likes++;
				Broadcast();
			}
		}
		else
		{
			try
			{
				target.Liked = true;
				target.Likes++;
				Broadcast();
				await ApiService.Notes.LikeNoteAsync(target.Id);
			}
			catch (ApiException)
			{
				target.Liked = false;
				target.Likes--;
				Broadcast();
			}
		}
	}

	private void OnNoteChanged(object? _, NoteResponse note)
	{
		NoteResponse = note;
		Rerender();
	}

	protected override void OnInitialized()
	{
		_noteChangedHandler = MessageSvc.Register(NoteResponse.Id, OnNoteChanged, MessageService.Type.Updated);
	}

	public void Reply()
	{
		var target = NoteResponse.Renote ?? NoteResponse;
		ComposeService.ComposeDialog?.OpenDialog(target);
	}

	public async Task Renote(NoteVisibility visibility)
	{
		var target = NoteResponse.Renote ?? NoteResponse;
		target.Renotes++;
		Broadcast();
		try
		{
			await ApiService.Notes.RenoteNoteAsync(target.Id, visibility);
		}
		catch (ApiException)
		{
			target.Renotes--;
			Broadcast();
		}

		Rerender();
	}

	public void DoQuote()
	{
		var target = NoteResponse.Renote ?? NoteResponse;
		ComposeService.ComposeDialog?.OpenDialog(null, target);
	}

	public async Task Redraft()
	{
		await ApiService.Notes.DeleteNoteAsync(NoteResponse.Id);
		ComposeService.ComposeDialog?.OpenDialogRedraft(NoteResponse);
	}
	
	public async Task Bite()
	{
		await ApiService.Notes.BiteNoteAsync(NoteResponse.Id);
	}

	public async Task Mute()
	{
		await ApiService.Notes.MuteNoteAsync(NoteResponse.Id);
	}

	private void Rerender()
	{
		_shouldRender = true;
		StateHasChanged();
		_shouldRender = false;
	}

	protected override bool ShouldRender()
	{
		return _shouldRender;
	}

	public async Task Delete()
	{
		await ApiService.Notes.DeleteNoteAsync(NoteResponse.Id);
		await MessageSvc.DeleteNoteAsync(NoteResponse);
	}

	private void ShowNote()
	{
		_overrideHide = !_overrideHide;
		Rerender();
	}

	public void Dispose()
	{
		_noteChangedHandler.Dispose();
	}
}
