using GameLogBook.Services;
using Microsoft.AspNetCore.Components;

namespace GameLogBook.Components.Elements.LocalImage;

public partial class LocalImage
{
    private string? previousImagePath;
    private string? imageSource;

    [Inject]
    private LocalImageService LocalImageService { get; set; } = null!;

    [Parameter]
    public string? ImagePath { get; set; }

    [Parameter]
    public string ImageClass { get; set; } = string.Empty;

    [Parameter]
    public string AltText { get; set; } = string.Empty;

    [Parameter]
    public RenderFragment? Placeholder { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        if (previousImagePath == ImagePath)
        {
            return;
        }

        previousImagePath = ImagePath;
        imageSource = await LocalImageService.GetImageSourceAsync(ImagePath);
    }
}
