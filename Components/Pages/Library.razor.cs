namespace GameLogBook.Components.Pages;

public partial class Library
{
    private readonly List<string> games = [];

    private bool isAddPopupOpen;
    private string newGameName = string.Empty;

    private void OpenAddPopup()
    {
        newGameName = string.Empty;
        isAddPopupOpen = true;
    }

    private void CloseAddPopup()
    {
        isAddPopupOpen = false;
    }

    private void SaveGame(string gameName)
    {
        string trimmedGameName = gameName.Trim();

        if (string.IsNullOrWhiteSpace(trimmedGameName))
        {
            return;
        }

        games.Add(trimmedGameName);
        CloseAddPopup();
    }
}