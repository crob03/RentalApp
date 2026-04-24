namespace RentalApp.Constants;

/// <summary>
/// Shell route constants used for programmatic navigation throughout the application.
/// </summary>
public static class Routes
{
    /// <summary>The absolute root route for the login page. Clears the navigation stack.</summary>
    public const string Login = "//login";

    /// <summary>The registered route name for the login page. Used for push navigation.</summary>
    public const string LoginPage = "LoginPage";

    /// <summary>The registered route name for the registration page.</summary>
    public const string Register = "RegisterPage";

    /// <summary>The registered route name for the main dashboard page.</summary>
    public const string Main = "MainPage";

    /// <summary>The registered route name for the temporary placeholder page.</summary>
    public const string Temp = "TempPage";
}
