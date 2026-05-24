using GameLogBook.Models.Games;
using Microsoft.AspNetCore.Components;

namespace GameLogBook.Components.Elements.GameElements;

public partial class GameCoverWidget : ComponentBase
{
    [Parameter]
    public Game Game { get; set; }
}