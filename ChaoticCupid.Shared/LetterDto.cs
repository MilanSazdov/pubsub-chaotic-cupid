namespace ChaoticCupid.Shared;

public sealed class LetterDto
{
    public string FromUsername { get; set; } = string.Empty;
    public string FromCity { get; set; } = string.Empty;
    public int FromAge { get; set; }

    // Null when the random message is "I am not interested in meeting."
    public string? FromPhoneNumber { get; set; }

    public string Message { get; set; } = string.Empty;
}
