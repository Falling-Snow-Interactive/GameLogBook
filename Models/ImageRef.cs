namespace VGL.Models;

public class ImageRef
{
    public string? Path { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? PendingUrl { get; set; }

    public bool IsValid() => !string.IsNullOrWhiteSpace(Path) || !string.IsNullOrWhiteSpace(PendingUrl);
}
