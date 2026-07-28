using Re.Domain.Enums;
using Xunit;

namespace Re.UnitTests;

public class TurkishLocalizationTests
{
    [Fact]
    public void AuditEventType_Should_Return_Correct_Turkish_String()
    {
        // Arrange
        var eventType = AuditEventType.UserLogin;

        // Act
        var result = eventType.ToTurkishString();

        // Assert
        Assert.Equal("Kullanıcı Girişi", result);
    }
}
