using System.Text.RegularExpressions;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.Services;

/// <summary>
/// This service is used to filter all notes that are streamed to the frontend through <see cref="StreamingService"/>. These notes are not filtered on the backend as they are rendered once and sent out to every applicable connected client at the same time for performance reasons.
/// </summary>
internal class FilterService : IDisposable
{
    private readonly ApiService       _api;
    private readonly SessionService   _session;
    private readonly StreamingService _streaming;

    private List<FilterResponse>? Filters { get; set; }

    public FilterService(ApiService api, SessionService session, StreamingService streaming)
    {
        _api       = api;
        _session   = session;
        _streaming = streaming;

        _streaming.FilterAdded   += OnFilterAdded;
        _streaming.FilterUpdated += OnFilterAdded;
        _streaming.FilterRemoved += OnFilterRemoved;
    }

    private void OnFilterAdded(object? _, FilterResponse filter)
    {
        Filters?.RemoveAll(p => p.Id == filter.Id);
        Filters?.Add(filter);
    }

    private void OnFilterRemoved(object? _, long id)
    {
        Filters?.RemoveAll(p => p.Id == id);
    }

    public async Task InitializeAsync()
    {
        Filters ??= (await _api.Filters.GetFiltersAsync()).ToList();
    }

    public void FilterNote(NoteResponse note, FilterResponse.FilterContext? context)
    {
        if (Filters == null) return;

        var filters = context is { } fc ? Filters.Where(p => p.Contexts.Contains(fc)).ToList() : Filters;

        if (filters.Count == 0) return;

        var filtered = IsFiltered(note, filters);
        if (filtered != null)
        {
            note.Filtered = new NoteFilteredSchema
            {
                Id      = filtered.Value.Item1.Id,
                Name    = filtered.Value.Item1.Name,
                Keyword = filtered.Value.Item2,
                Hide = filtered.Value.Item1.Action == FilterResponse.FilterAction.Hide
                       && note.User.Id != _session.Current?.Id
            };
        }
    }

    private static (FilterResponse, string)? IsFiltered(NoteResponse root, List<FilterResponse> filters)
    {
        var notes = new List<NoteBase?> { root, root.Reply, root.Renote };

        foreach (var note in notes)
        {
            if (note == null || (note.Text == null && note.Cw == null)) continue;

            foreach (var filter in filters.OrderBy(p => p.Action == FilterResponse.FilterAction.Warn))
            {
                var match = IsFiltered(note, filter);
                if (match != null) return (filter, match);
            }
        }

        return null;
    }

    private static string? IsFiltered(NoteBase note, FilterResponse filter)
    {
        foreach (var keyword in filter.Keywords)
        {
            if (keyword.StartsWith('"') && keyword.EndsWith('"'))
            {
                var pattern = $@"\b{keyword[1..^1]}\b";
                var regex   = new Regex(pattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(10));

                try
                {
                    if (note.Text != null && regex.IsMatch(note.Text) || note.Cw != null && regex.IsMatch(note.Cw))
                        return keyword;
                }
                catch (RegexMatchTimeoutException)
                {
                    return null;
                }
            }
            else if (keyword.StartsWith('/') && keyword.EndsWith('/'))
            {
                var regex = new Regex(keyword[1..^1], RegexOptions.IgnoreCase | RegexOptions.NonBacktracking,
                                      TimeSpan.FromMilliseconds(0.75));

                try
                {
                    if (note.Text != null && regex.IsMatch(note.Text) || note.Cw != null && regex.IsMatch(note.Cw))
                        return keyword;
                }
                catch (RegexMatchTimeoutException)
                {
                    return null;
                }
            }
            else if ((note.Text != null && note.Text.Contains(keyword, StringComparison.InvariantCultureIgnoreCase))
                     || (note.Cw != null && note.Cw.Contains(keyword, StringComparison.InvariantCultureIgnoreCase)))
            {
                return keyword;
            }
        }

        return null;
    }

    public void Dispose()
    {
        _streaming.FilterAdded   -= OnFilterAdded;
        _streaming.FilterUpdated -= OnFilterAdded;
        _streaming.FilterRemoved -= OnFilterRemoved;
    }
}
