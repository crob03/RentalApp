namespace RentalApp.Models;

public sealed record UpdateItemRequest(
    string? Title,
    string? Description,
    double? DailyRate,
    bool? IsAvailable
);
