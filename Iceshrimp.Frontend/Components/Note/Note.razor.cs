using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Components;

namespace Iceshrimp.Frontend.Components.Note;

public partial class Note : IDisposable
{
	[Inject] private ApiService        ApiService        { get; set; } = null!;
	[Inject] private NavigationManager NavigationManager { get; set; } = null!;
	[Inject] private ComposeService    ComposeService    { get; set; } = null!;
	[Inject] private MessageService    MessageSvc        { get; set; } = null!;

	[Parameter] [EditorRequired] public required NoteResponse NoteResponse { get; set; }
	[Parameter]                  public          bool         Indented     { get; set; }

	public void React(EmojiResponse emoji)
	{
		var target                             = NoteResponse.Renote ?? NoteResponse;
		var x                                  = target.Reactions.FirstOrDefault(p => p.Name == emoji.Name);
		if (x is null || x.Reacted == false) _ = AddReact(emoji.Name, emoji.PublicUrl);
	}

	public async Task AddReact(string name, string? url = null)
	{
		var target = NoteResponse.Renote ?? NoteResponse;
		var x      = target.Reactions.FirstOrDefault(p => p.Name == name);
		if (x == null)
		{
			target.Reactions.Add(new NoteReactionSchema
			{
				NoteId  = target.Id,
				Name    = name,
				Count   = 1,
				Reacted = true,
				Url     = url
			});
		}
		else x.Count++;

		Broadcast();
		try
		{
			await ApiService.Notes.ReactToNote(target.Id, name);
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
			await ApiService.Notes.RemoveReactionFromNote(target.Id, name);
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
		MessageSvc.UpdateNote(NoteResponse);
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
				await ApiService.Notes.UnlikeNote(target.Id);
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
				await ApiService.Notes.LikeNote(target.Id);
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
		StateHasChanged();
	}

	protected override void OnInitialized()
	{
		MessageSvc.Register(NoteResponse.Id, OnNoteChanged);
	}

	public void Reply()
	{
		var target = NoteResponse.Renote ?? NoteResponse;
		ComposeService.ComposeDialog?.OpenDialog(target);
	}

	public async Task Renote()
	{
		var target = NoteResponse.Renote ?? NoteResponse;
		target.Renotes++;
		Broadcast();
		try
		{
			await ApiService.Notes.RenoteNote(target.Id);
		}
		catch (ApiException)
		{
			target.Renotes--;
			Broadcast();
		}

		StateHasChanged();
	}

	public void DoQuote()
	{
		var target = NoteResponse.Renote ?? NoteResponse;
		ComposeService.ComposeDialog?.OpenDialog(null, target);
	}

	public async void Redraft()
	{
		await ApiService.Notes.DeleteNote(NoteResponse.Id);
		ComposeService.ComposeDialog?.OpenDialogRedraft(NoteResponse);
	}

	public async Task Delete()
	{
		await ApiService.Notes.DeleteNote(NoteResponse.Id);
	}

	public void Dispose()
	{
		MessageSvc.Unregister(NoteResponse.Id, OnNoteChanged);
	}
}