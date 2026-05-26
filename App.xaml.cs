using VGL.Diagnostics;

namespace VGL;

public partial class App : Application
{
    public App()
    {
        AppErrorLogger.Initialize();
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState) => new(new MainPage());
}
