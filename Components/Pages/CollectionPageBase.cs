using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
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

    private void SortItems()
    {
        Items = Items
                .OrderBy(GetSortKey, StringComparer.OrdinalIgnoreCase)
                .ToList();
    }
}
