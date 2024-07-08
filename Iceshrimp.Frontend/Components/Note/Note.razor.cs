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
        var x            = NoteResponse.Reactions.FirstOrDefault(p => p.Name == emoji.Name);
        if (x is null || x.Reacted == false) _ = AddReact(emoji.Name, emoji.PublicUrl);
    }
    public async Task AddReact(string name, string? url = null)
    {
        var x = NoteResponse.Reactions.FirstOrDefault(p => p.Name == name);
        if (x == null)
        {
            NoteResponse.Reactions.Add(new NoteReactionSchema
            {
                NoteId  = NoteResponse.Id,
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
            await ApiService.Notes.ReactToNote(NoteResponse.Id, name);
        }
        catch (ApiException)
        {
            if (x!.Count > 1) x.Count--;
            else NoteResponse.Reactions.Remove(x);
            Broadcast();
        }
    }

    public async Task RemoveReact(string name)
    {
        var rollback        = NoteResponse.Reactions.First(p => p.Name == name);
        if (rollback.Count > 1) rollback.Count--;
        else NoteResponse.Reactions.Remove(rollback);
        Broadcast();
        try
        {
            await ApiService.Notes.RemoveReactionFromNote(NoteResponse.Id, name);
        }
        catch (ApiException)
        {
            if (rollback.Count >= 1) rollback.Count++;
            else NoteResponse.Reactions.Add(rollback);
            Broadcast();
        }
    }
    
    private void Broadcast()
    {
        MessageSvc.UpdateNote(NoteResponse);
    }

    public async Task ToggleLike()
    {
        if (NoteResponse.Liked)
        {
            try
            {
                NoteResponse.Liked = false;
                NoteResponse.Likes--;
                Broadcast();
                await ApiService.Notes.UnlikeNote(NoteResponse.Id);
            }
            catch (ApiException)
            {
                NoteResponse.Liked = true;
                NoteResponse.Likes++;
                Broadcast();
            }
        }
        else
        {
            try
            {
                NoteResponse.Liked = true;
                NoteResponse.Likes++;
                Broadcast();
                await ApiService.Notes.LikeNote(NoteResponse.Id);
            }
            catch (ApiException)
            {
                NoteResponse.Liked = false;
                NoteResponse.Likes--;
                Broadcast();
            }
        }
    }

    private void OnNoteChanged(object? _, NoteResponse note)
    {
        Console.WriteLine($"Received callback for ID: {NoteResponse.Id}");
        NoteResponse = note;
        StateHasChanged();
    }

    protected override void OnInitialized()
    {
        MessageSvc.Register(NoteResponse.Id, OnNoteChanged);
    }

    public void Reply()
    {
        
        ComposeService.ComposeDialog?.OpenDialog(NoteResponse);
    }

    public async Task Renote()
    {
        NoteResponse.Renotes++;
        Broadcast();
        try
        {
            await ApiService.Notes.RenoteNote(NoteResponse.Id);
        }
        catch (ApiException)
        {
            NoteResponse.Renotes--;
            Broadcast();
        }
        StateHasChanged();
    }

    public void DoQuote()
    {
        ComposeService.ComposeDialog?.OpenDialog(null, NoteResponse);
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