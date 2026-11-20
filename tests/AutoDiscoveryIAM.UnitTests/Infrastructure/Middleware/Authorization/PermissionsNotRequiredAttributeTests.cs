using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Infrastructure.Middleware.Authorization;

public class PermissionsNotRequiredAttributeTests
{
    [Fact]
    public void Constructor_WithoutReason_ShouldKeepReasonNull()
    {
        var attribute = new PermissionsNotRequiredAttribute();

        attribute.Reason.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithReason_ShouldStoreReason()
    {
        const string reason = "login endpoint is public";

        var attribute = new PermissionsNotRequiredAttribute(reason);

        attribute.Reason.Should().Be(reason);
    }

    [Fact]
    public void AttributeUsage_ShouldAllowMethodAndClassTargets()
    {
        var usage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(
            typeof(PermissionsNotRequiredAttribute),
            typeof(AttributeUsageAttribute));

        usage.Should().NotBeNull();
        usage!.ValidOn.HasFlag(AttributeTargets.Method).Should().BeTrue();
        usage.ValidOn.HasFlag(AttributeTargets.Class).Should().BeTrue();
    }

    [Fact]
    public void AttributeUsage_ShouldNotAllowMultiple()
    {
        var usage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(
            typeof(PermissionsNotRequiredAttribute),
            typeof(AttributeUsageAttribute));

        usage.Should().NotBeNull();
        usage!.AllowMultiple.Should().BeFalse();
    }
}
