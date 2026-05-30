using Microsoft.AspNetCore.Components;
using VGL.Components.Popups;
using VGL.Models;
using VGL.Models.Companies;
using VGL.Models.Games;
using VGL.Services;
using Platform = VGL.Models.Platforms.Platform;

namespace VGL.Components.Elements.Logs;

public partial class LogCard : ComponentBase
{
    [Inject]
    protected PopupService PopupService { get; set; } = null!;
    
    [Parameter]
    public GameLog Log { get; set; }
    
    [Parameter]
    public List<Playthrough> Playthroughs { get; set; } = [];
    
    [Parameter]
    public List<Game> Games { get; set; } = [];
    
    [Parameter]
    public List<Platform> Platforms { get; set; } = [];

    [Parameter]
    public List<Company> Companies { get; set; } = [];
    
    private async Task OpenEditPopup()
    {
        GameLog editableLog = new(Log);

        GameLog? updatedLog = await PopupService.ShowAsync<AddGameLogPopup, GameLog>(
                                                                                     new Dictionary<string, object?>
                                                                                     {
                                                                                         [nameof(AddGameLogPopup.InitialLog)] = editableLog,
                                                                                         [nameof(AddGameLogPopup.Playthroughs)] = Playthroughs,
                                                                                         [nameof(AddGameLogPopup.LibraryGames)] = Games,
                                                                                         [nameof(AddGameLogPopup.Platforms)] = Platforms,
                                                                                         [nameof(AddGameLogPopup.OnGameAdded)] = new Func<Task<Game?>>(AddGameFromPicker),
                                                                                         [nameof(AddGameLogPopup.OnPlatformAdded)] = new Func<Task<PlatformModel?>>(AddPlatformFromPicker)
                                                                                     });

        if (updatedLog is not null)
        {
            await UpdateLog(updatedLog);
        }
    }
}