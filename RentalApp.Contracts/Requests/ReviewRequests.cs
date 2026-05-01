namespace RentalApp.Contracts.Requests;

public record GetReviewsRequest(int Page = 1, int PageSize = 10);

public record CreateReviewRequest(int RentalId, int Rating, string? Comment);
