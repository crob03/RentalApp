using RentalApp.Helpers;

namespace RentalApp.Test.Helpers;

public class RegistrationValidatorTests
{
    // Valid baseline inputs — all fields pass validation
    private const string ValidFirst = "Jane";
    private const string ValidLast = "Doe";
    private const string ValidEmail = "jane@example.com";
    private const string ValidPassword = "Password1!";
    private const string ValidConfirm = "Password1!";
    private const bool AcceptTerms = true;

    private static string? Validate(
        string firstName = ValidFirst,
        string lastName = ValidLast,
        string email = ValidEmail,
        string password = ValidPassword,
        string confirmPassword = ValidConfirm,
        bool acceptTerms = AcceptTerms
    ) =>
        RegistrationValidator.Validate(
            firstName,
            lastName,
            email,
            password,
            confirmPassword,
            acceptTerms
        );

    // ── Happy path ─────────────────────────────────────────────────────

    [Fact]
    public void Validate_AllValidInputs_ReturnsNull()
    {
        var result = Validate();

        Assert.Null(result);
    }

    // ── First name ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_BlankFirstName_ReturnsError(string firstName)
    {
        var result = Validate(firstName: firstName);

        Assert.Equal("First name is required", result);
    }

    [Fact]
    public void Validate_FirstNameExceeds50Chars_ReturnsError()
    {
        var result = Validate(firstName: new string('A', 51));

        Assert.Equal("First name must be 50 characters or fewer", result);
    }

    [Fact]
    public void Validate_FirstNameExactly50Chars_ReturnsNull()
    {
        var result = Validate(firstName: new string('A', 50));

        Assert.Null(result);
    }

    // ── Last name ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_BlankLastName_ReturnsError(string lastName)
    {
        var result = Validate(lastName: lastName);

        Assert.Equal("Last name is required", result);
    }

    [Fact]
    public void Validate_LastNameExceeds50Chars_ReturnsError()
    {
        var result = Validate(lastName: new string('A', 51));

        Assert.Equal("Last name must be 50 characters or fewer", result);
    }

    [Fact]
    public void Validate_LastNameExactly50Chars_ReturnsNull()
    {
        var result = Validate(lastName: new string('A', 50));

        Assert.Null(result);
    }

    // ── Email ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_BlankEmail_ReturnsError(string email)
    {
        var result = Validate(email: email);

        Assert.Equal("Email is required", result);
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@tld")]
    [InlineData("@nodomain.com")]
    [InlineData("spaces in@email.com")]
    public void Validate_InvalidEmailFormat_ReturnsError(string email)
    {
        var result = Validate(email: email);

        Assert.Equal("Please enter a valid email address", result);
    }

    // ── Password ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_BlankPassword_ReturnsError(string password)
    {
        var result = Validate(password: password, confirmPassword: password);

        Assert.Equal("Password is required", result);
    }

    [Fact]
    public void Validate_PasswordTooShort_ReturnsError()
    {
        var result = Validate(password: "Ab1!", confirmPassword: "Ab1!");

        Assert.Equal("Password must be at least 8 characters long", result);
    }

    [Theory]
    [InlineData("password1!")]
    [InlineData("PASSWORD1!")]
    [InlineData("Password12")]
    [InlineData("Password!!")]
    public void Validate_PasswordFailsComplexity_ReturnsError(string password)
    {
        var result = Validate(password: password, confirmPassword: password);

        Assert.Equal(
            "Password must contain an uppercase letter, lowercase letter, number, and special character",
            result
        );
    }

    // ── Confirm password ───────────────────────────────────────────────

    [Fact]
    public void Validate_PasswordMismatch_ReturnsError()
    {
        var result = Validate(confirmPassword: "Different1!");

        Assert.Equal("Passwords do not match", result);
    }

    // ── Terms ──────────────────────────────────────────────────────────

    [Fact]
    public void Validate_TermsNotAccepted_ReturnsError()
    {
        var result = Validate(acceptTerms: false);

        Assert.Equal("Please accept the terms and conditions", result);
    }
}
