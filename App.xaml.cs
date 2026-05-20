using GameLogBook.Diagnostics;

namespace GameLogBook;

public partial class App : Application
{
    public App()
    {
        AppErrorLogger.Initialize();
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState) => new(new MainPage());
}
