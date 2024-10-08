using Iceshrimp.Backend.Core.Middleware;
using static Iceshrimp.Backend.Pages.Shared.RootComponent;

namespace Iceshrimp.Backend.Components.Helpers;

[Authenticate("scope:admin")]
[RequireAuthorization]
public class AdminComponentBase : AsyncComponentBase;