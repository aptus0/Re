using Re.Application.Common.Interfaces;
using BCrypt.Net;

namespace Re.Infrastructure.Services;

/// <summary>
/// BCrypt ile güvenli şifre hashleme.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    public string Hash(string plainText) =>
        BCrypt.Net.BCrypt.HashPassword(plainText, workFactor: 12);

    public bool Verify(string plainText, string hash) =>
        BCrypt.Net.BCrypt.Verify(plainText, hash);
}

/// <summary>
/// UTC saat servisi.
/// </summary>
public class DateTimeService : IDateTimeService
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}

