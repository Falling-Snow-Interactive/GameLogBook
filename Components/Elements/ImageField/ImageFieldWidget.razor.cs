using GameLogBook.Models;
using GameLogBook.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace GameLogBook.Components.Elements.ImageField;

public partial class ImageFieldWidget : IAsyncDisposable
{
    private const string DefaultAccept = "image/png,image/jpeg,image/gif,image/webp";

    // Keeping track of variables
    private ImageRef? previousImageRef;
    private ImageRef? existingImageRef = null;
    
    // Input
    private string? urlInput = null;
    
    // Image
    private string? tempImagePath;
    private string? previewSource;
    
    // Error
    private string? errorMessage;
    
    // Controls
    private bool isBusy;
    
    // Reset
    private bool resetRequested;
    
    // Preview
    private bool isPreviewObserverConnected;
    
    // Elements
    private ElementReference widgetElement;
    private ElementReference controlsElement;

    // Inject
    [Inject]
    private LocalImageService LocalImageService { get; set; } = null!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    // Parameters
    [Parameter, EditorRequired]
    public string Label { get; set; } = "Image";

    [Parameter]
    public string FieldName { get; set; } = "image";

    [Parameter]
    public string Category { get; set; } = "images";

    [Parameter]
    public ImageRef? ImageRef { get; set; }

    [Parameter]
    public string? AltText { get; set; }

    [Parameter]
    public string InputClass { get; set; } = "game-detail-input";

    [Parameter]
    public string UrlPlaceholder { get; set; } = "Download image from URL";

    [Parameter]
    public string Accept { get; set; } = DefaultAccept;
    
    [Parameter, EditorRequired]
    public double AspectRatio { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private string RootClass => string.Join(' ',
                                            new[]
                                            {
                                                "form-field",
                                                AdditionalAttributes?.TryGetValue("class", out object? value) == true
                                                    ? value?.ToString()
                                                    : null
                                            }.Where(className => !string.IsNullOrWhiteSpace(className)));

    private Dictionary<string, object> AttributesWithoutClass =>
        AdditionalAttributes?
            .Where(attribute => attribute.Key != "class")
            .ToDictionary(attribute => attribute.Key, attribute => attribute.Value)
        ?? [];

    private string UrlInputId => $"{FieldName}-image-url";

    private bool IsWide => AspectRatio > 1;

    private string WidgetClass => IsWide ? "image-field-widget image-field-widget-wide" 
                                      : "image-field-widget image-field-widget-tall";

    private string WidgetStyle => $"--image-field-preview-aspect-width: {ToFractionalAspectRatio(AspectRatio).Width}; " +
                                  $"--image-field-preview-aspect-height: {ToFractionalAspectRatio(AspectRatio).Width};";

    private string PreviewAltText => string.IsNullOrWhiteSpace(AltText)
                                         ? $"{Label} preview"
                                         : AltText;

    protected override async Task OnParametersSetAsync()
    {
        if (previousImageRef == ImageRef)
        {
            return;
        }

        previousImageRef = ImageRef;

        await DeleteCurrentTempImage();
        
        resetRequested = false;

        // Image
        existingImageRef = ImageRef;
        urlInput = ImageRef?.PendingUrl;
        
        // Error
        errorMessage = null;
        
        // Preview
        previewSource = await LocalImageService.GetImageSourceAsync(existingImageRef?.Path);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await JsRuntime.InvokeVoidAsync("gameLogBookImageField.observe", widgetElement, controlsElement);
        isPreviewObserverConnected = true;
    }

    public async Task<ImageRef?> CommitAsync()
    {
        if (!string.IsNullOrWhiteSpace(tempImagePath))
        {
            resetRequested = false;

            if (existingImageRef is not null && tempImagePath is not null)
            {
                string finalImagePath = await LocalImageService.MoveTempImageAsync(tempImagePath, Category);
                existingImageRef.Path = finalImagePath;
            }
            
            tempImagePath = null;
            return existingImageRef;
        }

        if (resetRequested || string.IsNullOrWhiteSpace(existingImageRef?.Path))
        {
            return null;
        }

        return existingImageRef;
    }
    
    public async Task<string?> CommitPathAsync()
    {
        if (!string.IsNullOrWhiteSpace(tempImagePath))
        {
            resetRequested = false;

            if (existingImageRef is not null && tempImagePath is not null)
            {
                string finalImagePath = await LocalImageService.MoveTempImageAsync(tempImagePath, Category);
                existingImageRef.Path = finalImagePath;
            }
            
            tempImagePath = null;
            return existingImageRef?.Path;
        }

        if (resetRequested || string.IsNullOrWhiteSpace(existingImageRef?.Path))
        {
            return null;
        }

        return existingImageRef?.Path;
    }

    private void HandleUrlChanged(ChangeEventArgs args)
    {
        urlInput = args.Value?.ToString() ?? string.Empty;
        errorMessage = null;
    }

    private async Task DownloadUrlImage()
    {
        if (string.IsNullOrWhiteSpace(urlInput))
        {
            errorMessage = "Enter an image URL before downloading.";
            return;
        }

        await ReplaceTempImage(async () => await LocalImageService.DownloadTempImageAsync(urlInput));
    }

    private async Task UploadImage(InputFileChangeEventArgs args)
    {
        IBrowserFile file = args.File;
        urlInput = string.Empty;

        await ReplaceTempImage(async () => await LocalImageService.SaveUploadedTempImageAsync(file));
    }

    private async Task ResetImage()
    {
        await DeleteCurrentTempImage();

        existingImageRef = null;
        
        urlInput = string.Empty;
        previewSource = null;
        errorMessage = null;
        resetRequested = true;
    }

    private async Task ReplaceTempImage(Func<Task<string>> createTempImage)
    {
        isBusy = true;
        errorMessage = null;

        try
        {
            string nextTempImagePath = await createTempImage();
            string? nextPreviewSource = await LocalImageService.GetImageSourceAsync(nextTempImagePath);

            await DeleteCurrentTempImage();

            tempImagePath = nextTempImagePath;
            previewSource = nextPreviewSource;
            resetRequested = false;
        }
        catch (Exception exception)
        {
            errorMessage = exception.Message;
        }
        finally
        {
            isBusy = false;
        }
    }

    private async Task DeleteCurrentTempImage()
    {
        if (string.IsNullOrWhiteSpace(tempImagePath))
        {
            return;
        }

        await LocalImageService.DeleteTempImageAsync(tempImagePath);
        tempImagePath = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (isPreviewObserverConnected)
        {
            try
            {
                await JsRuntime.InvokeVoidAsync("gameLogBookImageField.cleanup", widgetElement);
            }
            catch (JSDisconnectedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        await DeleteCurrentTempImage();
    }
    
    public static (int Width, int Height) ToFractionalAspectRatio(double aspectRatio, int precision = 1000)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(aspectRatio);

        int numerator = (int)Math.Round(aspectRatio * precision);
        int gcd = GreatestCommonDivisor(numerator, precision);

        return (numerator / gcd, precision / gcd);
    }

    private static int GreatestCommonDivisor(int a, int b)
    {
        while (b != 0)
        {
            (a, b) = (b, a % b);
        }

        return Math.Abs(a);
    }
}
