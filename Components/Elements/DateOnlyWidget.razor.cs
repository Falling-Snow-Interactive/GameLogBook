using GameLogBook.Models.Games;
using Microsoft.AspNetCore.Components;

namespace GameLogBook.Components.Elements;

public partial class DateOnlyWidget : ComponentBase
{
    [Parameter]
    public Game Game { get; set; }

    [Parameter]
    public string Class { get; set; }
}