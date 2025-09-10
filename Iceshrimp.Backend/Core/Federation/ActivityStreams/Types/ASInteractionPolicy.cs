using Iceshrimp.Backend.Core.Configuration;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASInteractionPolicy
{
	[J($"{Constants.GoToSocialNs}#canQuote")]
	[JC(typeof(ASInteractionSubPolicyConverter))]
	public ASInteractionSubPolicy? CanQuote { get; set; }

	public ASInteractionPolicy ShallowClone()
	{
		return (ASInteractionPolicy) MemberwiseClone();
	}
}

public class ASInteractionPolicyConverter : ASSerializer.ListSingleObjectConverter<ASInteractionPolicy>;

public class ASInteractionSubPolicy
{
	[J($"{Constants.GoToSocialNs}#automaticApproval")]
	public List<ASObjectBase>? AutomaticApproval { get; set; }

	public ASInteractionSubPolicy ShallowClone()
	{
		return (ASInteractionSubPolicy) MemberwiseClone();
	}
}

public class ASInteractionSubPolicyConverter : ASSerializer.ListSingleObjectConverter<ASInteractionSubPolicy>;
