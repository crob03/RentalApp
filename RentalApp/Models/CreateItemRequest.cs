namespace RentalApp.Models;

public sealed record CreateItemRequest(
    string Title,
    string? Description,
    double DailyRate,
    int CategoryId,
    double Latitude,
    double Longitude
);
