using Microsoft.UI.Xaml;

namespace VGL.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        string userDataFolder = Path.Combine(FileSystem.AppDataDirectory, "WebView2");
        Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", userDataFolder);

        InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
