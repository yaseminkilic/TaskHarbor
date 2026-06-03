using System.Text.RegularExpressions;
using TaskTracker.Domain.Common;

namespace TaskTracker.Domain.Entities;

public class User
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled);

    public Guid Id { get; private set; }
    public string Email { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; }

    public ICollection<Project> Projects { get; private set; } = new List<Project>();

    private User() { }

    public User(string email, string displayName)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is not in a valid format.");

        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (!EmailRegex.IsMatch(normalizedEmail))
            throw new DomainException("Email is not in a valid format.");

        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("DisplayName is required.");

        Id = Guid.NewGuid();
        Email = normalizedEmail;
        DisplayName = displayName.Trim();
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Rename(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("DisplayName is required.");
        DisplayName = displayName.Trim();
    }
}
