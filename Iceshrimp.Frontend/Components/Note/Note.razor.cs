using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Iceshrimp.Frontend.Components.Note;

public partial class Note
{
	[Inject] private IStringLocalizer<Localization.Localization> Loc               { get; set; } = null!;

	[Parameter] [EditorRequired] public required NoteResponse NoteResponse { get; set; }
	[Parameter]                  public          bool         Indented     { get; set; }
	private                                      bool         _overrideHide       = false;
	
	
	private void ShowNote()
	{
		_overrideHide = !_overrideHide;
		StateHasChanged();
	}
	
}
