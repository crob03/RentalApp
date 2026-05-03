namespace RentalApp.Database.States;

/// <summary>Defines the possible lifecycle states of a rental.</summary>
public enum RentalStatus
{
    Requested,
    Approved,
    Rejected,
    OutForRent,
    Overdue,
    Returned,
    Completed,
}
