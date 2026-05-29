using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using VGL.Models.Games.Platforms;
using Platform = VGL.Models.Platforms.Platform;

namespace VGL.Components.Elements.GameElements;

public partial class OwnershipInputWidget : ComponentBase
{
    [Parameter]
    public IReadOnlyList<Platform> Platforms { get; set; } = [];

    [Parameter]
    public List<GamePlatformRelation> Ownerships { get; set; } = [];

    [Parameter]
    public EventCallback<List<GamePlatformRelation>> OwnershipsChanged { get; set; }

    private bool IsSelected(int platformId)
    {
        return Ownerships.Any(ownership => ownership.PlatformID == platformId);
    }

    private OwnershipType GetOwnershipType(int platformId)
    {
        return Ownerships.FirstOrDefault(ownership => ownership.PlatformID == platformId)?.Ownership
               ?? OwnershipType.Digital;
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

    private async Task ChangeOwnershipType(int platformId, string? value)
    {
        if (!Enum.TryParse(value, out OwnershipType ownershipType))
        {
            return;
        }

        GamePlatformRelation? ownership = Ownerships.FirstOrDefault(item => item.PlatformID == platformId);

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
            ownership.Ownership = ownershipType;
        }

        await NotifyChanged();
    }

    private async Task NotifyChanged()
    {
        Ownerships = Ownerships
                     .Where(ownership => ownership.PlatformID > 0)
                     .GroupBy(ownership => ownership.PlatformID)
                     .Select(group => group.First())
                     .OrderBy(ownership => ownership.PlatformID)
                     .ToList();

        await OwnershipsChanged.InvokeAsync(Ownerships);
    }
}
