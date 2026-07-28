using Re.Contracts.Common;
using Re.Domain.Enums;
using Xunit;

namespace Re.UnitTests;

public class DocumentStatusTests
{
    [Fact]
    public void DocumentStatus_Values_Should_Be_Defined()
    {
        // Assert
        Assert.Equal(0, (int)DocumentStatus.Draft);
        Assert.Equal(1, (int)DocumentStatus.Approved);
        Assert.Equal("Kullanıcı adı alanı boş bırakılamaz.", ValidationMessages.UsernameRequired);
    }
}
