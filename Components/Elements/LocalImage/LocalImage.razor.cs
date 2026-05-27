using Microsoft.AspNetCore.Components;
using VGL.Services;

namespace VGL.Components.Elements.LocalImage;

public partial class LocalImage
{
    private string? previousImagePath;
    private int? previousMaxWidth;
    private int? previousMaxHeight;
    private string? imageSource;
    private long imageLoadVersion;

    [Inject]
    private LocalImageService LocalImageService { get; set; } = null!;

    [Parameter]
    public string? ImagePath { get; set; }

    [Parameter]
    public string ImageClass { get; set; } = string.Empty;

    [Parameter]
    public string AltText { get; set; } = string.Empty;

    [Parameter]
    public string MissingText { get; set; } = "No image";

    [Parameter]
    public int? MaxWidth { get; set; }

    [Parameter]
    public int? MaxHeight { get; set; }

    [Parameter]
    public RenderFragment? Placeholder { get; set; }

    protected override Task OnParametersSetAsync()
    {
        if (previousImagePath == ImagePath
            && previousMaxWidth == MaxWidth
            && previousMaxHeight == MaxHeight)
        {
            return Task.CompletedTask;
        }

        previousImagePath = ImagePath;
        previousMaxWidth = MaxWidth;
        previousMaxHeight = MaxHeight;

        long loadVersion = ++imageLoadVersion;
        imageSource = null;
        _ = LoadImageSourceAsync(loadVersion, ImagePath, MaxWidth, MaxHeight);

        return Task.CompletedTask;
    }

    private async Task LoadImageSourceAsync(long loadVersion, string? imagePath, int? maxWidth, int? maxHeight)
    {
        string? nextImageSource;
        try
        {
            nextImageSource = await LocalImageService.GetImageSourceAsync(imagePath, maxWidth, maxHeight);
        }
        catch
        {
            nextImageSource = null;
        }

        if (loadVersion != imageLoadVersion)
        {
            return;
        }

        imageSource = nextImageSource;
        await InvokeAsync(StateHasChanged);
    }
}
