using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.DiscoveryPermissionsService;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Dtos;
using System.Reflection;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Services.PermissionNaming;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Permissions.Commands.SyncPermissions.Services.DiscoveryPermissionsService;

public class EndpointDiscoveryServiceTests
{
    [Fact]
    public async Task DiscoverEndpointsFromMetadataAsync_WhenEndpointDataSourceIsMissing_ShouldReturnEmptyList()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var namingService = Substitute.For<IPermissionNamingService>();
        serviceProvider.GetService(typeof(EndpointDataSource)).Returns(null);

        var sut = new EndpointDiscoveryService(serviceProvider, namingService);

        var result = await sut.DiscoverEndpointsFromMetadataAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DiscoverEndpointsFromMetadataAsync_WhenEndpointHasMetadata_ShouldCreatePermissionDescriptor()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var namingService = Substitute.For<IPermissionNamingService>();
        var endpointDataSource = new TestEndpointDataSource(CreateRouteEndpoint());

        serviceProvider.GetService(typeof(EndpointDataSource)).Returns(endpointDataSource);
        namingService.BuildPermissionNaming(Arg.Any<string>()).Returns(new PermissionNamingResultDto
        {
            Feature = "Posts",
            Action = "CreatePost",
            ActionSnakeCase = "create_post",
            CqrsType = "Command",
            Key = "posts.create_post",
            IsValid = true,
        });

        var sut = new EndpointDiscoveryService(serviceProvider, namingService);

        var result = await sut.DiscoverEndpointsFromMetadataAsync(CancellationToken.None);

        result.Should().HaveCount(1);
        var descriptor = result.Single();
        descriptor.Key.Should().Be("posts.create_post");
        descriptor.Feature.Should().Be("Posts");
        descriptor.Action.Should().Be("create_post");
        descriptor.CqrsType.Should().Be("Command");
        descriptor.HttpMethods.Should().ContainSingle().Which.Should().Be("POST");
        descriptor.Routes.Should().ContainSingle().Which.Should().Be("/api/test");
        descriptor.HasPermissionsNotRequired.Should().BeTrue();
        descriptor.PermissionsNotRequiredReason.Should().Be("public endpoint");
        descriptor.RequiresDevelopmentAccess.Should().BeTrue();
        descriptor.RequiresDevelopmentAccessReason.Should().Be("dev endpoint");
        descriptor.RequiresSuAdminAccess.Should().BeTrue();
        descriptor.RequiresSuAdminAccessReason.Should().Be("su endpoint");
        descriptor.HasRequirePermissions.Should().BeTrue();
        descriptor.RequirePermissionsNote.Should().Be("note endpoint");

        namingService.Received(1).BuildPermissionNaming(Arg.Any<string>());
    }

    [Fact]
    public async Task DiscoverEndpointsFromMetadataAsync_WhenServiceProviderThrows_ShouldReturnEmptyList()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var namingService = Substitute.For<IPermissionNamingService>();
        serviceProvider.When(x => x.GetService(typeof(EndpointDataSource))).Do(_ => throw new InvalidOperationException("boom"));

        var sut = new EndpointDiscoveryService(serviceProvider, namingService);

        var result = await sut.DiscoverEndpointsFromMetadataAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DiscoverEndpointsFromMetadataAsync_WhenEndpointIsNotRouteEndpoint_ShouldSkipIt()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var namingService = Substitute.For<IPermissionNamingService>();
        var endpoint = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "plain");
        var endpointDataSource = new TestEndpointDataSource(endpoint);

        serviceProvider.GetService(typeof(EndpointDataSource)).Returns(endpointDataSource);

        var sut = new EndpointDiscoveryService(serviceProvider, namingService);

        var result = await sut.DiscoverEndpointsFromMetadataAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DiscoverEndpointsFromMetadataAsync_WhenPatternIsEmpty_ShouldSkipEndpoint()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var namingService = Substitute.For<IPermissionNamingService>();
        var endpointDataSource = new TestEndpointDataSource(CreateRouteEndpoint(string.Empty));

        serviceProvider.GetService(typeof(EndpointDataSource)).Returns(endpointDataSource);
        namingService.BuildPermissionNaming(Arg.Any<string>()).Returns(new PermissionNamingResultDto
        {
            Feature = "Posts",
            Action = "CreatePost",
            CqrsType = "Command",
            IsValid = true,
        });

        var sut = new EndpointDiscoveryService(serviceProvider, namingService);

        var result = await sut.DiscoverEndpointsFromMetadataAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DiscoverEndpointsFromMetadataAsync_WhenNamingIsInvalid_ShouldSkipEndpoint()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var namingService = Substitute.For<IPermissionNamingService>();
        var endpointDataSource = new TestEndpointDataSource(CreateRouteEndpoint());

        serviceProvider.GetService(typeof(EndpointDataSource)).Returns(endpointDataSource);
        namingService.BuildPermissionNaming(Arg.Any<string>()).Returns(new PermissionNamingResultDto
        {
            IsValid = false,
            ErrorMessage = "invalid namespace",
        });

        var sut = new EndpointDiscoveryService(serviceProvider, namingService);

        var result = await sut.DiscoverEndpointsFromMetadataAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DiscoverEndpointsFromMetadataAsync_WhenHttpMethodMetadataIsMissing_ShouldDefaultToGet()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var namingService = Substitute.For<IPermissionNamingService>();
        var endpointDataSource = new TestEndpointDataSource(CreateRouteEndpoint("/api/default", includeHttpMethod: false));

        serviceProvider.GetService(typeof(EndpointDataSource)).Returns(endpointDataSource);
        namingService.BuildPermissionNaming(Arg.Any<string>()).Returns(new PermissionNamingResultDto
        {
            Feature = "Users",
            Action = "GetUsers",
            CqrsType = "Query",
            IsValid = true,
        });

        var sut = new EndpointDiscoveryService(serviceProvider, namingService);

        var result = await sut.DiscoverEndpointsFromMetadataAsync(CancellationToken.None);

        result.Should().ContainSingle();
        result.Single().HttpMethods.Should().ContainSingle().Which.Should().Be("GET");
    }

    [Fact]
    public async Task DiscoverEndpointsFromMetadataAsync_WhenAuthorizationMetadataIsMissing_ShouldLeaveFlagsDisabled()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var namingService = Substitute.For<IPermissionNamingService>();
        var endpointDataSource = new TestEndpointDataSource(CreateRouteEndpoint("/api/no-auth", includeHttpMethod: true, includeAuthorizationMetadata: false));

        serviceProvider.GetService(typeof(EndpointDataSource)).Returns(endpointDataSource);
        namingService.BuildPermissionNaming(Arg.Any<string>()).Returns(new PermissionNamingResultDto
        {
            Feature = "Users",
            Action = "ListUsers",
            CqrsType = "Query",
            IsValid = true,
        });

        var sut = new EndpointDiscoveryService(serviceProvider, namingService);

        var result = await sut.DiscoverEndpointsFromMetadataAsync(CancellationToken.None);

        result.Should().ContainSingle();
        var descriptor = result.Single();
        descriptor.HasPermissionsNotRequired.Should().BeFalse();
        descriptor.RequiresDevelopmentAccess.Should().BeFalse();
        descriptor.RequiresSuAdminAccess.Should().BeFalse();
        descriptor.HasRequirePermissions.Should().BeFalse();
        descriptor.PermissionsNotRequiredReason.Should().BeNull();
        descriptor.RequiresDevelopmentAccessReason.Should().BeNull();
        descriptor.RequiresSuAdminAccessReason.Should().BeNull();
        descriptor.RequirePermissionsNote.Should().BeNull();
    }

    [Fact]
    public async Task DiscoverEndpointsFromMetadataAsync_WhenRoutePatternRawTextIsNull_ShouldSkipEndpoint()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var namingService = Substitute.For<IPermissionNamingService>();
        var endpointDataSource = new TestEndpointDataSource(CreateRouteEndpointWithoutRawText());

        serviceProvider.GetService(typeof(EndpointDataSource)).Returns(endpointDataSource);

        var sut = new EndpointDiscoveryService(serviceProvider, namingService);

        var result = await sut.DiscoverEndpointsFromMetadataAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractPermissionFromEndpointAsync_WhenRoutePatternRawTextIsNull_ShouldReturnNull()
    {
        var namingService = Substitute.For<IPermissionNamingService>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var sut = new EndpointDiscoveryService(serviceProvider, namingService);
        var endpoint = CreateRouteEndpoint();

        typeof(RoutePattern)
            .GetField("<RawText>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(endpoint.RoutePattern, null);

        var method = typeof(EndpointDiscoveryService)
            .GetMethod("ExtractPermissionFromEndpointAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;

        var task = (Task<PermissionDescriptor?>)method.Invoke(sut, new object[] { endpoint, CancellationToken.None })!;
        var result = await task;

        result.Should().BeNull();
    }

    [Fact]
    public async Task ExtractPermissionFromEndpointAsync_WhenRoutePatternIsNull_ShouldReturnNull()
    {
        var namingService = Substitute.For<IPermissionNamingService>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var sut = new EndpointDiscoveryService(serviceProvider, namingService);
        var endpoint = CreateRouteEndpoint();

        typeof(RouteEndpoint)
            .GetField("<RoutePattern>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(endpoint, null);

        var method = typeof(EndpointDiscoveryService)
            .GetMethod("ExtractPermissionFromEndpointAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;

        var task = (Task<PermissionDescriptor?>)method.Invoke(sut, new object[] { endpoint, CancellationToken.None })!;
        var result = await task;

        result.Should().BeNull();
    }

    // Endpoints minimal-API reales traen un MethodInfo en sus metadatos automáticamente (issue #88:
    // EndpointDiscoveryService lo usa para resolver el namespace real vía MethodInfo.DeclaringType,
    // en vez del placeholder hardcodeado que tenía antes) -- RouteEndpointBuilder no lo agrega solo,
    // hay que simularlo explícitamente para que estos endpoints de prueba se comporten como uno real.
    private static void DummyHandler()
    {
    }

    private static RouteEndpoint CreateRouteEndpoint(string pattern = "/api/test", bool includeHttpMethod = true, bool includeAuthorizationMetadata = true)
    {
        var builder = new RouteEndpointBuilder(
          requestDelegate: _ => Task.CompletedTask,
          routePattern: RoutePatternFactory.Parse(pattern),
          order: 0);

        builder.Metadata.Add(((Action)DummyHandler).Method);

        if (includeHttpMethod)
        {
            builder.Metadata.Add(new HttpMethodMetadata(["POST"]));
        }
        if (includeAuthorizationMetadata)
        {
            builder.Metadata.Add(new PermissionsNotRequiredAttribute("public endpoint"));
            builder.Metadata.Add(new RequiresDevelopmentAccessAttribute("dev endpoint"));
            builder.Metadata.Add(new RequiresSuAdminAccessAttribute("su endpoint"));
            builder.Metadata.Add(new RequirePermissionsAttribute("note endpoint"));
        }

        return (RouteEndpoint)builder.Build();
    }

    private static RouteEndpoint CreateRouteEndpointWithoutRawText()
    {
        var routePattern = RoutePatternFactory.Parse("/api/nullraw");
        typeof(RoutePattern)
            .GetField("<RawText>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(routePattern, null);

        var builder = new RouteEndpointBuilder(
            requestDelegate: _ => Task.CompletedTask,
            routePattern: routePattern,
            order: 0);

        return (RouteEndpoint)builder.Build();
    }

    private sealed class TestEndpointDataSource(params Endpoint[] endpoints) : EndpointDataSource
    {
        private readonly IReadOnlyList<Endpoint> _endpoints = endpoints;

        public override IReadOnlyList<Endpoint> Endpoints => _endpoints;

        public override IChangeToken GetChangeToken() => new CancellationChangeToken(CancellationToken.None);
    }
}
