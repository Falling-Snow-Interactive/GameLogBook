using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using VGL.Components.Elements.CollectionPageShell;
using VGL.Data;
using VGL.Services.UserProfiles;

namespace VGL.Components.Pages;

public abstract class CollectionPageBase<TEntity> : ComponentBase
    where TEntity : class
{
    [Inject]
    protected GameLogBookDbContext DbContext { get; set; } = null!;

    [Inject]
    protected UserProfileSession UserSession { get; set; } = null!;

    [Inject]
    protected NavigationManager Navigation { get; set; } = null!;

    protected List<TEntity> Items { get; set; } = [];

    protected CollectionViewMode ViewMode { get; private set; } = CollectionViewMode.Card;

    protected abstract DbSet<TEntity> EntitySet { get; }

    protected abstract string GetSortKey(TEntity item);

    protected virtual IQueryable<TEntity> BuildQuery()
    {
        return EntitySet;
    }

    protected override async Task OnInitializedAsync()
    {
        if (!await EnsureSignedInAsync())
        {
            return;
        }

        LoadViewModePreference();
        await LoadItemsAsync();
    }

    protected virtual async Task LoadItemsAsync()
    {
        Items = (await BuildQuery().ToListAsync())
                .OrderBy(GetSortKey, StringComparer.OrdinalIgnoreCase)
                .ToList();
    }

    protected async Task<bool> EnsureSignedInAsync()
    {
        if (UserSession.IsSignedIn || await UserSession.TryAutoSignInAsync())
        {
            return true;
        }

        Navigation.NavigateTo("/profiles");
        return false;
    }

    protected virtual Task OpenAddPopup()
    {
        return Task.CompletedTask;
    }

    protected async Task AddItemAsync(TEntity item)
    {
        EntitySet.Add(item);
        await DbContext.SaveChangesAsync();

        Items.Add(item);
        SortItems();
    }

    protected async Task UpdateItemAsync()
    {
        await DbContext.SaveChangesAsync();
        await LoadItemsAsync();
    }

    protected async Task RemoveItemAsync(TEntity item)
    {
        EntitySet.Remove(item);
        await DbContext.SaveChangesAsync();

        Items.Remove(item);
    }

    protected void SetViewMode(CollectionViewMode viewMode)
    {
        ViewMode = viewMode;

        if (UserSession.CurrentUserID is not null)
        {
            Preferences.Default.Set(GetViewPreferenceKey(), viewMode.ToString());
        }
    }

    protected static string FormatDate(DateOnly? date)
    {
        return date?.ToString("MMM d, yyyy") ?? "Not set";
    }

    protected static string FormatNames(IEnumerable<string> names)
    {
        string[] normalizedNames = names
                                   .Where(name => !string.IsNullOrWhiteSpace(name))
                                   .Distinct(StringComparer.OrdinalIgnoreCase)
                                   .Order(StringComparer.OrdinalIgnoreCase)
                                   .ToArray();

        return normalizedNames.Length == 0
                   ? "None"
                   : string.Join(", ", normalizedNames);
    }

    private void LoadViewModePreference()
    {
        if (UserSession.CurrentUserID is null)
        {
            ViewMode = CollectionViewMode.Card;
            return;
        }

        string storedViewMode = Preferences.Default.Get(GetViewPreferenceKey(), nameof(CollectionViewMode.Card));

        ViewMode = Enum.TryParse(storedViewMode, ignoreCase: true, out CollectionViewMode parsedViewMode)
                       ? parsedViewMode
                       : CollectionViewMode.Card;
    }

    private string GetViewPreferenceKey()
    {
        return $"CollectionViewMode:{GetType().Name}:{UserSession.CurrentUserID}";
    }

    private void SortItems()
    {
        Items = Items
                .OrderBy(GetSortKey, StringComparer.OrdinalIgnoreCase)
                .ToList();
    }
}
