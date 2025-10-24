using System.Diagnostics;
using System.Diagnostics.Metrics;
using Iceshrimp.Shared.Helpers;

namespace Iceshrimp.Backend.Core.Helpers;

/// <summary>
/// https://opentelemetry.io/docs/languages/dotnet/instrumentation/#setting-up-an-activitysource
/// 
/// It is recommended to use a custom type to hold references for ActivitySource.
/// This avoids possible type collisions with other components in the DI container.
/// </summary>
public static class Telemetry
{
    public const string Source = "Iceshrimp.NET";

    public static ActivitySource ActivitySource { get; } = new(Source, VersionHelpers.VersionInfo.Value.Version);
    public static Meter          Meter          { get; } = new(Source, VersionHelpers.VersionInfo.Value.Version);

    public static void Dispose()
    {
        ActivitySource.Dispose();
        Meter.Dispose();
    }
}