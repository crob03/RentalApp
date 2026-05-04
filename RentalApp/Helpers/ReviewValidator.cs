namespace RentalApp.Helpers;

public static class ReviewValidator
{
    public static string? Validate(int rating, string? comment)
    {
        if (rating < 1 || rating > 5)
            return "Rating must be between 1 and 5.";
        if (comment is not null && comment.Length > 500)
            return "Comment must be 500 characters or fewer.";
        return null;
    }
}
