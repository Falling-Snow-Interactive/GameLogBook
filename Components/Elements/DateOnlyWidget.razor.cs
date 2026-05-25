using Microsoft.AspNetCore.Components;

namespace GameLogBook.Components.Elements;

public partial class DateOnlyWidget : ComponentBase
{
    [Parameter]
    public DateOnly? Date { get; set; }

    [Parameter]
    public string Class { get; set; }

    public string GetCleanDate()
    {
        return Date == null ? string.Empty : Date.Value.ToString("MMMM d, yyyy");
    }
}