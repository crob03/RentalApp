namespace RentalApp.Constants;

/// <summary>
/// Shell route constants used for programmatic navigation throughout the application.
/// </summary>
public static class Routes
{
    /// <summary>The absolute root route for the login page. Clears the navigation stack.</summary>
    public const string Login = "//login";

    /// <summary>The registered route name for the registration page.</summary>
    public const string Register = "RegisterPage";

    /// <summary>The absolute root route for the main dashboard page. Clears the navigation stack.</summary>
    public const string Main = "//main";

    /// <summary>The registered route name for the temporary placeholder page.</summary>
    public const string Temp = "TempPage";

    /// <summary>The registered route name for the items list page.</summary>
    public const string ItemsList = "ItemsListPage";

    /// <summary>The registered route name for the item details page.</summary>
    public const string ItemDetails = "ItemDetailsPage";

    /// <summary>The registered route name for the create item page.</summary>
    public const string CreateItem = "CreateItemPage";

    /// <summary>The registered route name for the nearby items page.</summary>
    public const string NearbyItems = "NearbyItemsPage";

    /// <summary>The registered route name for the rentals page.</summary>
    public const string Rentals = "RentalsPage";

    /// <summary>The registered route name for the manage rental page.</summary>
    public const string ManageRental = "ManageRentalPage";

    /// <summary>The registered route name for the create review page.</summary>
    public const string CreateReview = "CreateReviewPage";

    /// <summary>The registered route name for the user profile page.</summary>
    public const string UserProfile = "UserProfilePage";
}
