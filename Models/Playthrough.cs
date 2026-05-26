namespace VGL.Models;

public class Playthrough
{
    public int ID { get; set; }

    public string Name { get; set; } = string.Empty;

    public int[] GameIds { get; set; } = [];
}