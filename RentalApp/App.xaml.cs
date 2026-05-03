namespace RentalApp;

/// <summary>
/// The root application class. Provides the application window backed by <see cref="AppShell"/>.
/// </summary>
public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>Initialises a new instance of <see cref="App"/>.</summary>
    /// <param name="serviceProvider">
    /// The application service provider, used to resolve <see cref="AppShell"/> at window creation time.
    /// </param>
    public App(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        InitializeComponent();
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
