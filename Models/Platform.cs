namespace GameLogBook.Models;

public class Platform
{
    public int id { get; set; }

    public string name { get; set; }

    public int[] games { get; set; } = [];
}