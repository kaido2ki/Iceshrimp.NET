using Iceshrimp.Backend.Core.Extensions;
using JetBrains.Annotations;

namespace Iceshrimp.Backend.Core.Services;

[UsedImplicitly]
public class FlagService : ISingletonService
{
	public AsyncLocal<bool> SupportsHtmlFormatting { get; } = new();
	public AsyncLocal<bool> SupportsInlineMedia    { get; } = new();
	public AsyncLocal<bool> IsPleroma              { get; } = new();
}
