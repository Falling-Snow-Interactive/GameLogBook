using Microsoft.AspNetCore.Components;

namespace GameLogBook.Components.Elements.CompanyElements;

public partial class CompaniesWidget : ComponentBase
{
    [Parameter]
    public IReadOnlyList<string?> DeveloperNames { get; set; }
    
    [Parameter]
    public IReadOnlyList<string?> PublisherNames { get; set; }
}