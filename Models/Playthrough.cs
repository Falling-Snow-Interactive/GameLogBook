namespace GameLogBook.Models;

public class Playthrough
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int[] GameIds { get; set; } = [];
}