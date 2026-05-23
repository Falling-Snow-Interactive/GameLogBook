namespace GameLogBook.Models.Games;

public class Cover
{
    public string? ImagePath { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? PendingImageUrl { get; set; }
}
