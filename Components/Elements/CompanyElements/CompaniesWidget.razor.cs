using Microsoft.AspNetCore.Components;

namespace VGL.Components.Elements.CompanyElements;

public partial class CompaniesWidget : ComponentBase
{
    [Parameter]
    public IReadOnlyList<string?> DeveloperNames { get; set; } = [];
    
    [Parameter]
    public IReadOnlyList<string?> PublisherNames { get; set; } = [];

    [Parameter]
    public string LabelClass { get; set; } = string.Empty;
}
