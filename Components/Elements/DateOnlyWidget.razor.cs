using Microsoft.AspNetCore.Components;

namespace VGL.Components.Elements;

public partial class DateOnlyWidget : ComponentBase
{
    [Parameter]
    public DateOnly? Date { get; set; }

    [Parameter]
    public string Class { get; set; } = string.Empty;

    public string GetCleanDate()
    {
        return Date == null ? string.Empty : Date.Value.ToString("MMMM d, yyyy");
    }
}
