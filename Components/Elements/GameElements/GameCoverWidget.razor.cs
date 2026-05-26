using Microsoft.AspNetCore.Components;
using VGL.Models.Games;

namespace VGL.Components.Elements.GameElements;

public partial class GameCoverWidget : ComponentBase
{
    [Parameter]
    public Game Game { get; set; }
}