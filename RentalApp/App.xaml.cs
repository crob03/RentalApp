namespace RentalApp;

/// <summary>
/// The root application class. Registers all Shell routes on startup and provides the
/// application window backed by <see cref="AppShell"/>.
/// </summary>
public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initialises a new instance of <see cref="App"/>, registering all Shell navigation routes.
    /// </summary>
    /// <param name="serviceProvider">
    /// The application service provider, used to resolve <see cref="AppShell"/> at window creation time.
    /// </param>
    public App(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        InitializeComponent();

        Routing.RegisterRoute(nameof(Views.MainPage), typeof(Views.MainPage));
        Routing.RegisterRoute(nameof(Views.LoginPage), typeof(Views.LoginPage));
        Routing.RegisterRoute(nameof(Views.RegisterPage), typeof(Views.RegisterPage));
        Routing.RegisterRoute(nameof(Views.TempPage), typeof(Views.TempPage));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Resolves <see cref="AppShell"/> from the DI container rather than constructing it directly,
    /// ensuring its dependencies are injected correctly.
    /// </remarks>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var shell =
            _serviceProvider.GetService<AppShell>()
            ?? throw new InvalidOperationException(
                "AppShell could not be resolved from the service provider."
            );

        return new Window(shell);
    }
}
