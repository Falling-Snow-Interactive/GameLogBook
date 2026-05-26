using Microsoft.AspNetCore.Components.Web;
using VGL.Diagnostics;

namespace VGL.Components.Elements;

public class LoggedErrorBoundary : ErrorBoundary
{
    protected override Task OnErrorAsync(Exception exception)
    {
        AppErrorLogger.Log("Blazor.ErrorBoundary", exception);
        return base.OnErrorAsync(exception);
    }
}
