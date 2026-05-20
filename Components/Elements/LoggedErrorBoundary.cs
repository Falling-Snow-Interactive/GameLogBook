using GameLogBook.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace GameLogBook.Components.Elements;

public class LoggedErrorBoundary : ErrorBoundary
{
    protected override Task OnErrorAsync(Exception exception)
    {
        AppErrorLogger.Log("Blazor.ErrorBoundary", exception);
        return base.OnErrorAsync(exception);
    }
}
