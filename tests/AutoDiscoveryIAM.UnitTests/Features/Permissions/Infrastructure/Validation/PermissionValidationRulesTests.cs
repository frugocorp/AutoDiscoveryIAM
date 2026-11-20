using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Infrastructure.Validation;
using FluentValidation;
using FluentValidation.TestHelper;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Permissions.Infrastructure.Validation;

public class PermissionValidationRulesTests
{
    private readonly PermissionValidationModelValidator _validator = new();

    [Fact]
    public void ValidateSyncRequest_ShouldFail_WhenRequestIsNull()
    {
        var model = CreateValidModel();
        model.Request = null!;

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Request);
    }

    [Fact]
    public void ValidateSyncRequest_ShouldFail_WhenDryRunAndForceFullSyncAreBothTrue()
    {
        var model = CreateValidModel();
        model.Request = new SyncPermissionsRequestDto(ForceFullSync: true, DryRun: true);

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Request);
    }

    [Theory]
    [InlineData("users.create_user")]
    [InlineData("roles.assign_permission")]
    [InlineData("users.*")]
    [InlineData("*.read")]
    public void ValidatePermissionKey_ShouldPass_WhenFormatIsValid(string key)
    {
        var model = CreateValidModel();
        model.Key = key;

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.Key);
    }

    [Theory]
    [InlineData("")]
    [InlineData("users")]
    [InlineData("users.create-user")]
    [InlineData("users.*.x")]
    public void ValidatePermissionKey_ShouldFail_WhenFormatIsInvalid(string key)
    {
        var model = CreateValidModel();
        model.Key = key;

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Key);
    }

    [Fact]
    public void ValidatePermissionDescription_ShouldFail_WhenTooLong()
    {
        var model = CreateValidModel();
        model.Description = new string('d', 501);

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void ValidatePermissionGroup_ShouldFail_WhenTooLong()
    {
        var model = CreateValidModel();
        model.Group = new string('g', 101);

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Group);
    }

    [Fact]
    public void ValidatePermissionRoute_ShouldFail_WhenTooLong()
    {
        var model = CreateValidModel();
        model.Route = new string('r', 501);

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Route);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("PATCH")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    public void ValidatePermissionHttpMethod_ShouldPass_WhenMethodIsAllowed(string method)
    {
        var model = CreateValidModel();
        model.HttpMethod = method;

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.HttpMethod);
    }

    [Fact]
    public void ValidatePermissionHttpMethod_ShouldFail_WhenMethodIsInvalid()
    {
        var model = CreateValidModel();
        model.HttpMethod = "TRACE";

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.HttpMethod);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Command")]
    [InlineData("Query")]
    public void ValidatePermissionCqrsType_ShouldPass_WhenValueIsAllowed(string? cqrsType)
    {
        var model = CreateValidModel();
        model.CqrsType = cqrsType;

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.CqrsType);
    }

    [Fact]
    public void ValidatePermissionCqrsType_ShouldFail_WhenValueIsInvalid()
    {
        var model = CreateValidModel();
        model.CqrsType = "Event";

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.CqrsType);
    }

    private static PermissionValidationModel CreateValidModel()
    {
        return new PermissionValidationModel
        {
            Request = new SyncPermissionsRequestDto(),
            Key = "users.create_user",
            Description = "Can create users",
            Group = "Users",
            Route = "/api/users",
            HttpMethod = "POST",
            CqrsType = "Command",
        };
    }

    private sealed class PermissionValidationModelValidator : AbstractValidator<PermissionValidationModel>
    {
        public PermissionValidationModelValidator()
        {
            RuleFor(x => x.Request).ValidateSyncRequest();
            RuleFor(x => x.Key).ValidatePermissionKey();
            RuleFor(x => x.Description).ValidatePermissionDescription();
            RuleFor(x => x.Group).ValidatePermissionGroup();
            RuleFor(x => x.Route).ValidatePermissionRoute();
            RuleFor(x => x.HttpMethod).ValidatePermissionHttpMethod();
            RuleFor(x => x.CqrsType).ValidatePermissionCqrsType();
        }
    }

    private sealed class PermissionValidationModel
    {
        public SyncPermissionsRequestDto Request { get; set; } = new();

        public string Key { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Group { get; set; }

        public string? Route { get; set; }

        public string? HttpMethod { get; set; }

        public string? CqrsType { get; set; }
    }
}
