using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace Iceshrimp.Backend.Controllers.Shared.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class ProducesErrorsAttribute(HttpStatusCode statusCode, params HttpStatusCode[] additional)
	: ProducesResponseTypeAttribute((int)statusCode)
{
	public IEnumerable<HttpStatusCode> StatusCodes => additional.Prepend(statusCode);
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class ProducesResultsAttribute(HttpStatusCode statusCode, params HttpStatusCode[] additional)
	: ProducesResponseTypeAttribute((int)statusCode)
{
	public IEnumerable<HttpStatusCode> StatusCodes => additional.Prepend(statusCode);
}

public abstract class OverrideResultTypeAttribute(Type type) : Attribute
{
	public Type Type => type;
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class OverrideResultTypeAttribute<T>() : OverrideResultTypeAttribute(typeof(T));

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class ProducesActivityStreamsPayload() : ProducesAttribute("application/activity+json",
                                                                  "application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\"");