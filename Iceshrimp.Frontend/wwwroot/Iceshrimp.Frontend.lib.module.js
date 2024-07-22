// This is a Blazor JavaScript Initializer.
// https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/startup?view=aspnetcore-8.0#javascript-initializers

export function afterStarted(blazor) {
    DisableHistory();
}

// Automatic attempts at restoring scroll history cause problems in Webkit, this is done manually in all relevant places.
export function DisableHistory(){
    history.scrollRestoration = "manual";
}