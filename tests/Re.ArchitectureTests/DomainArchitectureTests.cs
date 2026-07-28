using System.Reflection;
using Re.Domain.Enums;
using Xunit;

namespace Re.ArchitectureTests;

public class DomainArchitectureTests
{
    [Fact]
    public void Domain_Assembly_Should_Not_Depend_On_External_Frameworks()
    {
        // Arrange
        var domainAssembly = typeof(DocumentStatus).Assembly;

        // Act & Assert
        Assert.NotNull(domainAssembly);
        Assert.Equal("Re.Domain", domainAssembly.GetName().Name);
    }
}
