namespace RentalApp.Database.States;

/// <summary>
/// Maps a stored rental status string to the corresponding <see cref="IRentalState"/> instance.
/// </summary>
public static class RentalStateFactory
{
    /// <summary>
    /// Returns the state object for the given status string.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown for unknown status strings.</exception>
    public static IRentalState FromString(string status) =>
        status switch
        {
            "Requested" => new RequestedState(),
            "Approved" => new ApprovedState(),
            "Rejected" => new RejectedState(),
            "OutForRent" => new OutForRentState(),
            "Overdue" => new OverdueState(),
            "Returned" => new ReturnedState(),
            "Completed" => new CompletedState(),
            _ => throw new InvalidOperationException($"Unknown rental status: {status}."),
        };
}
