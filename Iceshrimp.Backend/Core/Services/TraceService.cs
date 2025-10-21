using System.Diagnostics;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Shared.Helpers;

namespace Iceshrimp.Backend.Core.Services;

/// <summary>
/// https://opentelemetry.io/docs/languages/dotnet/instrumentation/#setting-up-an-activitysource
/// 
/// It is recommended to use a custom type to hold references for ActivitySource.
/// This avoids possible type collisions with other components in the DI container.
/// </summary>
public class TraceService : IDisposable, ISingletonService
{
    public const string Source = "Iceshrimp.NET";

    public ActivitySource ActivitySource { get; } = new(Source, VersionHelpers.VersionInfo.Value.Version);

    public void Dispose()
    {
        ActivitySource.Dispose();
    }
}