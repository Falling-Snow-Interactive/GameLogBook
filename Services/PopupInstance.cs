using Microsoft.AspNetCore.Components;

namespace VGL.Services;

public sealed class PopupInstance
{
    private readonly TaskCompletionSource<object?> completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public PopupInstance(Type componentType, IDictionary<string, object?>? parameters = null)
    {
        ComponentType = componentType;
        Parameters = parameters is null
                         ? new Dictionary<string, object?>()
                         : new Dictionary<string, object?>(parameters);
    }

    public Type ComponentType { get; }

    public IDictionary<string, object?> Parameters { get; }

    internal Task<object?> ResultTask => completionSource.Task;

    public Task CloseAsync(object? result = null)
    {
        completionSource.TrySetResult(result);
        return Task.CompletedTask;
    }

    public static PopupInstance Create<TComponent>(IDictionary<string, object?>? parameters = null)
        where TComponent : IComponent
    {
        return new PopupInstance(typeof(TComponent), parameters);
    }
}
