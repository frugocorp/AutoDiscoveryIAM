using System.Reflection;
using System.Security.Claims;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security.SecurityEvents;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.AuthContext;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Services.UserQuery;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Infrastructure.Middleware.Authorization;

public class AuthorizationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithoutEndpoint_ShouldContinuePipeline()
    {
        var fixture = CreateFixture();
        var context = CreateContext(isAuthenticated: false);

        await fixture.Middleware.InvokeAsync(context);

        fixture.NextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithPermissionsNotRequiredMetadata_ShouldContinuePipeline()
    {
        var fixture = CreateFixture();
        var context = CreateContext(
            isAuthenticated: false,
            endpoint: CreateEndpoint(
                metadata: new PermissionsNotRequiredAttribute("public endpoint")));

        await fixture.Middleware.InvokeAsync(context);

        fixture.NextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithProtectedEndpointAndAnonymousUser_ShouldThrowAuthenticationRequired()
    {
        var fixture = CreateFixture();
        var context = CreateContext(isAuthenticated: false, endpoint: CreateEndpoint());

        var act = () => fixture.Middleware.InvokeAsync(context);

        await ((Func<Task>)(act)).Should().ThrowAsync<AuthenticationRequiredException>();
        fixture.NextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WhenUserPublicIdCannotBeResolved_ShouldThrowAuthenticationRequired()
    {
        var fixture = CreateFixture();
        fixture.AuthContextService.GetCurrentUserPublicId().Returns((Guid?)null);

        var context = CreateContext(isAuthenticated: true, endpoint: CreateEndpoint());

        var act = () => fixture.Middleware.InvokeAsync(context);

        await ((Func<Task>)(act)).Should().ThrowAsync<AuthenticationRequiredException>();
        fixture.NextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WhenCurrentUserCannotBeResolved_ShouldThrowAuthenticationRequired()
    {
        var fixture = CreateFixture();
        fixture.AuthContextService.GetCurrentUserAsync().Returns(Task.FromResult<User?>(null));

        var context = CreateContext(isAuthenticated: true, endpoint: CreateEndpoint());

        var act = () => fixture.Middleware.InvokeAsync(context);

        await ((Func<Task>)(act)).Should().ThrowAsync<AuthenticationRequiredException>();
        fixture.NextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WhenCurrentUserIsInactive_ShouldThrowInsufficientPermissions()
    {
        var fixture = CreateFixture();
        fixture.AuthContextService.GetCurrentUserAsync().Returns(Task.FromResult<User?>(new User { IsActive = false }));

        var context = CreateContext(isAuthenticated: true, endpoint: CreateEndpoint());

        var act = () => fixture.Middleware.InvokeAsync(context);

        await ((Func<Task>)(act)).Should().ThrowAsync<InsufficientPermissionsException>();
        fixture.NextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WhenAdminUserAndNoGranularRestriction_ShouldBypassPermissionValidation()
    {
        var fixture = CreateFixture();
        fixture.AuthContextService.GetCurrentUserAsync().Returns(Task.FromResult<User?>(new User { IsActive = true, IsAdmin = true }));

        var context = CreateContext(isAuthenticated: true, endpoint: CreateEndpoint());

        await fixture.Middleware.InvokeAsync(context);

        fixture.NextCalled.Should().BeTrue();
        await fixture.UserQueryService.DidNotReceiveWithAnyArgs().HasPermissionByPublicIdAsync(default, string.Empty);
    }

    [Fact]
    public async Task InvokeAsync_WhenEndpointRequiresDevelopmentAndUserIsNotDevelopment_ShouldThrowDevelopmentAccessRequired()
    {
        var fixture = CreateFixture();
        fixture.AuthContextService.GetCurrentUserAsync().Returns(Task.FromResult<User?>(new User { IsActive = true, IsAdmin = true }));

        var context = CreateContext(
            isAuthenticated: true,
            endpoint: CreateEndpoint(metadata: new RequiresDevelopmentAccessAttribute("only dev")));

        var act = () => fixture.Middleware.InvokeAsync(context);

        await ((Func<Task>)(act)).Should().ThrowAsync<DevelopmentAccessRequiredException>();
        fixture.NextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WhenEndpointRequiresSuAdminAndUserIsOnlyAdmin_ShouldThrowSuperAdminAccessRequired()
    {
        var fixture = CreateFixture();
        fixture.AuthContextService.GetCurrentUserAsync().Returns(Task.FromResult<User?>(new User { IsActive = true, IsAdmin = true }));

        var context = CreateContext(
            isAuthenticated: true,
            endpoint: CreateEndpoint(metadata: new RequiresSuAdminAccessAttribute("critical admin endpoint")));

        var act = () => fixture.Middleware.InvokeAsync(context);

        await ((Func<Task>)(act)).Should().ThrowAsync<SuperAdminAccessRequiredException>();
        fixture.NextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WhenEndpointRequiresSuAdminAndUserIsSuAdmin_ShouldProceed()
    {
        var fixture = CreateFixture();
        fixture.AuthContextService.GetCurrentUserAsync().Returns(Task.FromResult<User?>(new User { IsActive = true, IsSuAdmin = true }));

        var context = CreateContext(
            isAuthenticated: true,
            endpoint: CreateEndpoint(metadata: new RequiresSuAdminAccessAttribute("critical admin endpoint")));

        await fixture.Middleware.InvokeAsync(context);

        fixture.NextCalled.Should().BeTrue();
    }

    /// <summary>
    /// Regresión #081: si el resolvedor no puede derivar una Key para el endpoint, la
    /// autorización debe fallar cerrado (InsufficientPermissions), no dejar pasar la petición.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WhenNoRouteMatches_ShouldThrowInsufficientPermissions()
    {
        var fixture = CreateFixture();
        fixture.RouteCache
            .ResolveKey(Arg.Any<Endpoint>())
            .Returns((string?)null);

        var context = CreateContext(
            isAuthenticated: true,
            endpoint: CreateEndpoint(methodInfo: typeof(TestEndpointType).GetMethod(nameof(TestEndpointType.Get))));

        var act = () => fixture.Middleware.InvokeAsync(context);

        await ((Func<Task>)(act)).Should().ThrowAsync<InsufficientPermissionsException>();
        fixture.NextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WhenUserLacksPermission_ShouldThrowInsufficientPermissions()
    {
        var fixture = CreateFixture();
        fixture.UserQueryService.HasPermissionByPublicIdAsync(Arg.Any<Guid>(), Arg.Any<string>()).Returns(false);

        var context = CreateContext(
            isAuthenticated: true,
            endpoint: CreateEndpoint(methodInfo: typeof(TestEndpointType).GetMethod(nameof(TestEndpointType.Get))));

        var act = () => fixture.Middleware.InvokeAsync(context);

        await ((Func<Task>)(act)).Should().ThrowAsync<InsufficientPermissionsException>();
        fixture.NextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WhenUserHasPermission_ShouldProceed()
    {
        var fixture = CreateFixture();
        fixture.UserQueryService.HasPermissionByPublicIdAsync(Arg.Any<Guid>(), Arg.Any<string>()).Returns(true);

        var context = CreateContext(
            isAuthenticated: true,
            endpoint: CreateEndpoint(methodInfo: typeof(TestEndpointType).GetMethod(nameof(TestEndpointType.Get))));

        await fixture.Middleware.InvokeAsync(context);

        fixture.NextCalled.Should().BeTrue();
    }

    /// <summary>
    /// Regresión #081: confirma que el middleware consulta la caché de rutas con el método y la
    /// ruta reales de la petición (no reflexión de Type.Namespace).
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ShouldResolvePermissionKeyFromTheMatchedEndpoint()
    {
        var fixture = CreateFixture();
        var context = CreateContext(
            isAuthenticated: true,
            endpoint: CreateEndpoint(methodInfo: typeof(TestEndpointType).GetMethod(nameof(TestEndpointType.Get))));
        context.Request.Method = "GET";
        context.Request.Path = "/test-endpoint";

        await fixture.Middleware.InvokeAsync(context);

        fixture.RouteCache.Received(1).ResolveKey(context.GetEndpoint()!);
    }

    private static DefaultHttpContext CreateContext(bool isAuthenticated, Endpoint? endpoint = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/test-endpoint";

        var identity = isAuthenticated
            ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }, "TestAuth")
            : new ClaimsIdentity();

        context.User = new ClaimsPrincipal(identity);

        if (endpoint != null)
        {
            context.SetEndpoint(endpoint);
        }

        return context;
    }

    private static Endpoint CreateEndpoint(MethodInfo? methodInfo = null, params object[] metadata)
    {
        var builder = new RouteEndpointBuilder(
            requestDelegate: _ => Task.CompletedTask,
            routePattern: RoutePatternFactory.Parse("/test-endpoint"),
            order: 0);

        if (methodInfo != null)
        {
            builder.Metadata.Add(methodInfo);
        }

        foreach (var item in metadata)
        {
            builder.Metadata.Add(item);
        }

        return builder.Build();
    }

    private static MiddlewareFixture CreateFixture()
    {
        var authContextService = Substitute.For<IAuthContextService>();
        var userQueryService = Substitute.For<IUserQueryService>();
        var routeCache = Substitute.For<IEndpointPermissionKeyResolver>();

        var userPublicId = Guid.NewGuid();
        authContextService.GetCurrentUserPublicId().Returns(userPublicId);
        authContextService.GetCurrentUserAsync().Returns(Task.FromResult<User?>(new User { IsActive = true }));

        userQueryService.HasPermissionByPublicIdAsync(Arg.Any<Guid>(), Arg.Any<string>()).Returns(true);

        routeCache
            .ResolveKey(Arg.Any<Endpoint>())
            .Returns("posts.get_posts");

        var services = new ServiceCollection();
        services.AddScoped(_ => authContextService);
        services.AddScoped(_ => userQueryService);

        var provider = services.BuildServiceProvider();

        var fixture = new MiddlewareFixture
        {
            AuthContextService = authContextService,
            UserQueryService = userQueryService,
            RouteCache = routeCache,
        };

        RequestDelegate next = _ =>
        {
            fixture.NextCalled = true;
            return Task.CompletedTask;
        };

        fixture.Middleware = new AuthorizationMiddleware(next, NullLogger<AuthorizationMiddleware>.Instance, provider, Substitute.For<ISecurityEventLogger>(), routeCache);
        return fixture;
    }

    private sealed class MiddlewareFixture
    {
        public AuthorizationMiddleware Middleware { get; set; } = null!;

        public IAuthContextService AuthContextService { get; set; } = null!;

        public IUserQueryService UserQueryService { get; set; } = null!;

        public IEndpointPermissionKeyResolver RouteCache { get; set; } = null!;

        public bool NextCalled { get; set; }
    }

    private static class TestEndpointType
    {
        public static IResult Get() => Results.Ok();
    }
}
