namespace GameLogBook.Models.Games;

public class Image
{
    public string? ImagePath { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? PendingImageUrl { get; set; }
}
