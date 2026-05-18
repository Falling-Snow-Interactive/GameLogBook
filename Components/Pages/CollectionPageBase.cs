using GameLogBook.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace GameLogBook.Components.Pages;

public abstract class CollectionPageBase<TEntity> : ComponentBase
    where TEntity : class
{
    [Inject]
    protected GameLogBookDbContext DbContext { get; set; } = null!;

    protected List<TEntity> Items { get; private set; } = [];

    protected bool IsAddPopupOpen { get; private set; }

    protected abstract DbSet<TEntity> EntitySet { get; }

    protected abstract string GetSortKey(TEntity item);

    protected virtual IQueryable<TEntity> BuildQuery()
    {
        return EntitySet;
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadItemsAsync();
    }

    protected async Task LoadItemsAsync()
    {
        Items = (await BuildQuery().ToListAsync())
                .OrderBy(GetSortKey, StringComparer.OrdinalIgnoreCase)
                .ToList();
    }

    protected void OpenAddPopup()
    {
        IsAddPopupOpen = true;
    }

    protected virtual void CloseAddPopup()
    {
        IsAddPopupOpen = false;
    }

    protected async Task AddItemAsync(TEntity item)
    {
        EntitySet.Add(item);
        await DbContext.SaveChangesAsync();

        Items.Add(item);
        SortItems();
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
