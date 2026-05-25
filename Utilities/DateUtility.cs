namespace GameLogBook.Utilities;

public static class DateUtility
{
    public static string DateOnlyToCleanString(DateOnly? date)
    {
        return date == null ? string.Empty : date.Value.ToString("MMMM d, yyyy");
    }
}