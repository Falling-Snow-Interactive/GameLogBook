using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using VGL.Models.Games.Platforms;
using Platform = VGL.Models.Platforms.Platform;

namespace VGL.Components.Elements.GameElements;

public partial class OwnershipInputWidget : ComponentBase
{
    private ElementReference ownershipGridElement;
    private bool shouldAnimateLayout;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [Parameter]
    public IReadOnlyList<Platform> Platforms { get; set; } = [];

    [Parameter]
    public List<GamePlatformRelation> Ownerships { get; set; } = [];

    [Parameter]
    public EventCallback<List<GamePlatformRelation>> OwnershipsChanged { get; set; }

    private IEnumerable<Platform> OrderedPlatforms =>
        Platforms
            .Select((platform, index) => new
                                         {
                                             Platform = platform,
                                             Index = index
                                         })
            .OrderBy(item => IsSelected(item.Platform.ID) ? 0 : 1)
            .ThenBy(item => item.Index)
            .Select(item => item.Platform);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!shouldAnimateLayout)
        {
            return;
        }

        shouldAnimateLayout = false;
        await JsRuntime.InvokeVoidAsync("gameLogBookOwnershipInput.animate", ownershipGridElement);
    }

    private bool IsSelected(int platformId)
    {
        return Ownerships.Any(ownership => ownership.PlatformID == platformId);
    }

    private bool HasOwnershipType(int platformId, OwnershipType ownershipType)
    {
        return Ownerships.Any(ownership => ownership.PlatformID == platformId
                                           && ownership.Ownership == ownershipType);
    }

    private string GetOwnershipOptionClass(int platformId, OwnershipType ownershipType)
    {
        return HasOwnershipType(platformId, ownershipType)
                   ? "ownership-type-option ownership-type-option-selected"
                   : "ownership-type-option";
    }

    private Task TogglePlatform(int platformId)
    {
        return TogglePlatform(platformId, !IsSelected(platformId));
    }

    private async Task HandleRowKeyDown(int platformId, KeyboardEventArgs args)
    {
        if (args.Key is " " or "Enter")
        {
            await TogglePlatform(platformId);
        }
    }

    private async Task TogglePlatform(int platformId, bool isSelected)
    {
        await PrepareLayoutAnimation();

        if (isSelected && !IsSelected(platformId))
        {
            Ownerships.Add(new GamePlatformRelation
                           {
                               PlatformID = platformId,
                               Ownership = OwnershipType.Digital
                           });
        }
        else if (!isSelected)
        {
            Ownerships.RemoveAll(ownership => ownership.PlatformID == platformId);
        }

        await NotifyChanged();
    }

    private async Task ToggleOwnershipType(int platformId, OwnershipType ownershipType)
    {
        await PrepareLayoutAnimation();

        GamePlatformRelation? ownership = Ownerships.FirstOrDefault(item => item.PlatformID == platformId
                                                                            && item.Ownership == ownershipType);

        if (ownership is null)
        {
            Ownerships.Add(new GamePlatformRelation
                           {
                               PlatformID = platformId,
                               Ownership = ownershipType
                           });
        }
        else
        {
            Ownerships.Remove(ownership);
        }

        await NotifyChanged();
    }

    private async Task NotifyChanged()
    {
        Ownerships = Ownerships
                     .Where(ownership => ownership.PlatformID > 0)
                     .Where(ownership => ownership.Ownership != OwnershipType.None)
                     .GroupBy(ownership => new
                                           {
                                               ownership.PlatformID,
                                               ownership.Ownership
                                           })
                     .Select(group => group.First())
                     .OrderBy(ownership => ownership.PlatformID)
                     .ThenBy(ownership => ownership.Ownership)
                     .ToList();

        await OwnershipsChanged.InvokeAsync(Ownerships);
    }

    private async Task PrepareLayoutAnimation()
    {
        shouldAnimateLayout = true;
        await JsRuntime.InvokeVoidAsync("gameLogBookOwnershipInput.prepare", ownershipGridElement);
    }
}
