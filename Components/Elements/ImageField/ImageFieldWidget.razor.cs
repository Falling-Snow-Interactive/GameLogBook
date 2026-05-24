using GameLogBook.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace GameLogBook.Components.Elements.ImageField;

public partial class ImageFieldWidget : IAsyncDisposable
{
    private const string DefaultAccept = "image/png,image/jpeg,image/gif,image/webp";

    private string? previousImagePath;
    private string? previousPendingImageUrl;
    private string existingImagePath = string.Empty;
    private string urlInput = string.Empty;
    private string? tempImagePath;
    private string? previewSource;
    private string? errorMessage;
    private bool isBusy;
    private bool resetRequested;

    [Inject]
    private LocalImageService LocalImageService { get; set; } = null!;

    [Parameter]
    public string Label { get; set; } = "Image";

    [Parameter]
    public string FieldName { get; set; } = "image";

    [Parameter]
    public string Category { get; set; } = "images";

    [Parameter]
    public string? ImagePath { get; set; }

    [Parameter]
    public string? PendingImageUrl { get; set; }

    [Parameter]
    public string? AltText { get; set; }

    [Parameter]
    public string InputClass { get; set; } = "game-detail-input";

    [Parameter]
    public string UrlPlaceholder { get; set; } = "Download image from URL";

    [Parameter]
    public string Accept { get; set; } = DefaultAccept;

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

    private string PreviewAltText => string.IsNullOrWhiteSpace(AltText)
                                         ? $"{Label} preview"
                                         : AltText;

    protected override async Task OnParametersSetAsync()
    {
        if (previousImagePath == ImagePath && previousPendingImageUrl == PendingImageUrl)
        {
            return;
        }

        previousImagePath = ImagePath;
        previousPendingImageUrl = PendingImageUrl;

        await DeleteCurrentTempImage();

        existingImagePath = ImagePath ?? string.Empty;
        urlInput = PendingImageUrl ?? string.Empty;
        resetRequested = false;
        errorMessage = null;
        previewSource = await LocalImageService.GetImageSourceAsync(existingImagePath);
    }

    public async Task<string?> CommitAsync()
    {
        if (!string.IsNullOrWhiteSpace(tempImagePath))
        {
            string finalImagePath = await LocalImageService.MoveTempImageAsync(tempImagePath, Category);
            tempImagePath = null;
            existingImagePath = finalImagePath;
            resetRequested = false;

            return finalImagePath;
        }

        if (resetRequested || string.IsNullOrWhiteSpace(existingImagePath))
        {
            return null;
        }

        return existingImagePath;
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

        existingImagePath = string.Empty;
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
        await DeleteCurrentTempImage();
    }
}
