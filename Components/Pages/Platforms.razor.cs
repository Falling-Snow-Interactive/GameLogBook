using GameLogBook.Models.Companies;
using GameLogBook.Models.Games;
using Microsoft.EntityFrameworkCore;
using PlatformModel = GameLogBook.Models.Platforms.Platform;

namespace GameLogBook.Components.Pages;

public partial class Platforms : CollectionPageBase<PlatformModel>
{
    private List<Game> games = [];
    private List<Company> companies = [];
    private PlatformModel? selectedPlatform;

    protected override DbSet<PlatformModel> EntitySet => DbContext.Platforms;

    protected override string GetSortKey(PlatformModel item)
    {
        return item.Name;
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        games = await DbContext.Games
                               .AsNoTracking()
                               .OrderBy(game => game.Name)
                               .ToListAsync();

        companies = await DbContext.Companies
                                   .AsNoTracking()
                                   .OrderBy(company => company.Name)
                                   .ToListAsync();
    }

    private static string GetLinkedGameSummary(PlatformModel platform)
    {
        int linkedGameCount = platform.GameIds?.Length ?? 0;
        return linkedGameCount == 0
                   ? "No linked games"
                   : $"{linkedGameCount} linked game{(linkedGameCount == 1 ? string.Empty : "s")}";
    }

    private async Task AddPlatform(PlatformModel platform)
    {
        await AddItemAsync(platform);
        CloseAddPopup();
    }

    private async Task UpdatePlatform(PlatformModel updatedPlatform)
    {
        if (selectedPlatform is null)
        {
            return;
        }

        PlatformModel? existingPlatform = await DbContext.Platforms
                                                         .FirstOrDefaultAsync(platform => platform.ID == selectedPlatform.ID);

        if (existingPlatform is null)
        {
            CloseEditPopup();
            return;
        }

        existingPlatform.IgdbId = updatedPlatform.IgdbId;
        existingPlatform.Name = updatedPlatform.Name.Trim();
        existingPlatform.Abbreviation = updatedPlatform.Abbreviation.Trim();
        existingPlatform.CoverUrl = string.IsNullOrWhiteSpace(updatedPlatform.CoverUrl)
                                        ? null
                                        : updatedPlatform.CoverUrl.Trim();
        existingPlatform.ReleaseDate = updatedPlatform.ReleaseDate;
        existingPlatform.ManufacturerIds = updatedPlatform.ManufacturerIds ?? [];
        existingPlatform.GameIds = updatedPlatform.GameIds ?? [];

        await UpdateItemAsync();
        CloseEditPopup();
    }

    private async Task RemovePlatform(PlatformModel platform)
    {
        await RemoveItemAsync(platform);
    }

    private void OpenEditPopup(PlatformModel platform)
    {
        selectedPlatform = new PlatformModel(platform);
    }

    private void CloseEditPopup()
    {
        selectedPlatform = null;
    }
}
