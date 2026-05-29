using Microsoft.AspNetCore.Components;
using PlatformModel = VGL.Models.Platforms.Platform;

namespace VGL.Components.Elements.PlatformElements;

public partial class PlatformSearch : ComponentBase
{
    private const int FocusOutDelayMilliseconds = 250;

    private bool isDropdownActive;
    private int focusChangeVersion;
    private readonly List<PlatformModel> addedPlatforms = [];

    [Parameter]
    public IReadOnlyList<PlatformModel> Platforms { get; set; } = [];

    [Parameter]
    public string Placeholder { get; set; } = "Search platforms...";

    [Parameter]
    public int? SelectedPlatformId { get; set; }

    [Parameter]
    public EventCallback<int?> SelectedPlatformIdChanged { get; set; }

    [Parameter]
    public Func<Task<PlatformModel?>>? OnPlatformAdded { get; set; }

    private string SearchText { get; set; } = string.Empty;

    private bool HasSearchText => !string.IsNullOrWhiteSpace(SearchText);

    private bool ShouldShowDropdown => isDropdownActive && HasSearchText;

    private PlatformModel? SelectedPlatform => SelectedPlatformId is null
                                                   ? null
                                                   : AllPlatforms.FirstOrDefault(platform => platform.ID == SelectedPlatformId.Value);

    private IEnumerable<PlatformModel> AllPlatforms => Platforms
                                                       .Concat(addedPlatforms)
                                                       .DistinctBy(platform => platform.ID);

    private IReadOnlyList<PlatformModel> PlatformMatches => HasSearchText
                                                               ? FilterPlatforms(SearchText)
                                                               : [];

    private IReadOnlyList<PlatformModel> FilterPlatforms(string searchText)
    {
        string trimmedSearchText = searchText.Trim();

        return AllPlatforms
               .Where(platform => platform.ID != SelectedPlatformId)
               .Where(platform => platform.Name.Contains(trimmedSearchText, StringComparison.OrdinalIgnoreCase)
                                  || (platform.ShortName?.Contains(trimmedSearchText, StringComparison.OrdinalIgnoreCase) ?? false))
               .OrderBy(platform => platform.Name)
               .Take(10)
               .ToList();
    }

    private async Task SelectPlatform(PlatformModel platform)
    {
        await SelectedPlatformIdChanged.InvokeAsync(platform.ID);
        SearchText = string.Empty;
        isDropdownActive = false;
    }

    private async Task RemovePlatform()
    {
        await SelectedPlatformIdChanged.InvokeAsync(null);
    }

    private async Task HandlePlusClicked()
    {
        MarkDropdownActive();

        if (OnPlatformAdded is null)
        {
            return;
        }

        PlatformModel? platform = await OnPlatformAdded.Invoke();

        if (platform is null)
        {
            return;
        }

        AddOrUpdateAddedPlatform(platform);
        await SelectedPlatformIdChanged.InvokeAsync(platform.ID);
        SearchText = string.Empty;
        isDropdownActive = false;
    }

    private void AddOrUpdateAddedPlatform(PlatformModel platform)
    {
        int existingIndex = addedPlatforms.FindIndex(existingPlatform => existingPlatform.ID == platform.ID);

        if (existingIndex >= 0)
        {
            addedPlatforms[existingIndex] = platform;
            return;
        }

        addedPlatforms.Add(platform);
    }

    private async Task OnSearchTextChanged(ChangeEventArgs args)
    {
        MarkDropdownActive();
        SearchText = args.Value?.ToString() ?? string.Empty;
        await InvokeAsync(StateHasChanged);
    }

    private void HandleSearchFocusIn()
    {
        MarkDropdownActive();
    }

    private async Task HandleSearchFocusOut()
    {
        int currentFocusChangeVersion = ++focusChangeVersion;
        await Task.Delay(FocusOutDelayMilliseconds);

        if (currentFocusChangeVersion != focusChangeVersion)
        {
            return;
        }

        isDropdownActive = false;
        await InvokeAsync(StateHasChanged);
    }

    private void MarkDropdownActive()
    {
        focusChangeVersion++;
        isDropdownActive = true;
    }

    private static string GetPlatformBadge(PlatformModel platform)
    {
        if (!string.IsNullOrWhiteSpace(platform.ShortName) && platform.ReleaseDate is not null)
        {
            return $"{platform.ShortName} · {platform.ReleaseDate:MMM d, yyyy}";
        }

        if (!string.IsNullOrWhiteSpace(platform.ShortName))
        {
            return platform.ShortName;
        }

        return platform.ReleaseDate is null ? "Platform" : platform.ReleaseDate.Value.ToString("MMM d, yyyy");
    }
}
