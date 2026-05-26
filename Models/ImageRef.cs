namespace VGL.Models;

public class ImageRef
{
    public string? Path { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? PendingUrl { get; set; }
}
