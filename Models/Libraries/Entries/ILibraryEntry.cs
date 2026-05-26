namespace GameLogBook.Models.Libraries.Entries;

public interface ILibraryEntry
{
    public int ID { get; }
    
    public string Name { get; }
    public string? Summary { get; }
    
    public DateOnly? Date { get; }
    
    public ImageRef? Cover { get; }
    public ImageRef? Hero { get; }
    public ImageRef? Logo { get; }
    public ImageRef? Icon { get; }
}