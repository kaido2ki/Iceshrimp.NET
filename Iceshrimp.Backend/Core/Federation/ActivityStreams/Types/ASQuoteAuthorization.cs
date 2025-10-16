using Iceshrimp.Backend.Core.Configuration;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASQuoteAuthorization : ASObjectWithId
{
	public ASQuoteAuthorization() => Type = $"{Constants.FepNs}/044f#QuoteAuthorization";
	
	[J($"{Constants.ActivityStreamsNs}#attributedTo")]
	[JC(typeof(ASObjectBaseConverter))]
	public ASObjectBase? AttributedTo { get; set; }
	
	[J($"{Constants.GoToSocialNs}#interactingObject")]
	[JC(typeof(ASObjectBaseConverter))]
	public ASObjectBase? InteractingObject { get; set; }
	
	[J($"{Constants.GoToSocialNs}#interactionTarget")]
	[JC(typeof(ASObjectBaseConverter))]
	public ASObjectBase? InteractionTarget { get; set; }
}

public class ASQuoteAuthorizationConverter : ASSerializer.ListSingleObjectConverter<ASQuoteAuthorization>;