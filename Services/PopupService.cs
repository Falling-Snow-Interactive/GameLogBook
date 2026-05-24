using Microsoft.AspNetCore.Components;

namespace GameLogBook.Services;

public sealed class PopupService
{
    private readonly List<PopupInstance> popups = [];

    public event Action? Changed;

    public IReadOnlyList<PopupInstance> Popups => popups;

    public async Task<TResult?> ShowAsync<TComponent, TResult>(IDictionary<string, object?>? parameters = null)
        where TComponent : IComponent
    {
        PopupInstance popup = PopupInstance.Create<TComponent>(parameters);

        popups.Add(popup);
        NotifyChanged();

        try
        {
            object? result = await popup.ResultTask;

            return result switch
            {
                null => default,
                TResult typedResult => typedResult,
                _ => throw new InvalidOperationException(
                    $"{popup.ComponentType.Name} closed with a {result.GetType().Name} result, but {typeof(TResult).Name} was expected.")
            };
        }
        finally
        {
            popups.Remove(popup);
            NotifyChanged();
        }
    }

    public Task ShowAsync<TComponent>(IDictionary<string, object?>? parameters = null)
        where TComponent : IComponent
    {
        return ShowAsync<TComponent, object?>(parameters);
    }

    private void NotifyChanged()
    {
        Changed?.Invoke();
    }
}
