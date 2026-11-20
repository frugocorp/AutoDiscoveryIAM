using System.Text;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Logging.Logger;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization.Exceptions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Exceptions;
using AwesomeAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Infrastructure.Middleware.Exceptions;

public class ExceptionHandlingMiddlewareTests
{
  private static IWebHostEnvironment CreateEnvironment(string environmentName)
  {
    var environment = Substitute.For<IWebHostEnvironment>();
    environment.EnvironmentName = environmentName;
    return environment;
  }

  [Fact]
  public async Task Invoke_WhenAuthenticationRequiredException_ShouldWriteAuthorizationResponse()
  {
    var logger = Substitute.For<ILoggerBase<ExceptionHandlingMiddleware>>();

    RequestDelegate next = _ => throw new AuthenticationRequiredException("auth required");
    var middleware = new ExceptionHandlingMiddleware(logger, next, CreateEnvironment(Environments.Production));

    var context = new DefaultHttpContext();
    context.Response.Body = new MemoryStream();

    await middleware.Invoke(context);

    context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    context.Response.ContentType.Should().Contain("application/json");

    context.Response.Body.Position = 0;
    var body = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();
    body.Should().Contain("auth required");

    logger.DidNotReceiveWithAnyArgs().LogError(default!, default);
  }

  [Fact]
  public async Task Invoke_WhenUnhandledExceptionInDevelopment_ShouldIncludeExceptionDetailInResponse()
  {
    var logger = Substitute.For<ILoggerBase<ExceptionHandlingMiddleware>>();

    RequestDelegate next = _ => throw new InvalidOperationException("boom");
    var middleware = new ExceptionHandlingMiddleware(logger, next, CreateEnvironment(Environments.Development));

    var context = new DefaultHttpContext();
    context.Response.Body = new MemoryStream();

    await middleware.Invoke(context);

    context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    context.Response.ContentType.Should().Contain("application/json");

    context.Response.Body.Position = 0;
    var body = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();
    body.Should().Contain("boom");

    logger.Received(1).LogError(Arg.Is<string>(m => m.Contains("Error inesperado")), Arg.Any<Exception>());
  }

  [Fact]
  public async Task Invoke_WhenUnhandledExceptionOutsideDevelopment_ShouldNotLeakExceptionDetailInResponse()
  {
    var logger = Substitute.For<ILoggerBase<ExceptionHandlingMiddleware>>();

    RequestDelegate next = _ => throw new InvalidOperationException("boom");
    var middleware = new ExceptionHandlingMiddleware(logger, next, CreateEnvironment(Environments.Production));

    var context = new DefaultHttpContext();
    context.Response.Body = new MemoryStream();

    await middleware.Invoke(context);

    context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

    context.Response.Body.Position = 0;
    var body = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();
    body.Should().NotContain("boom");
    body.Should().NotContain(nameof(ExceptionHandlingMiddlewareTests));

    // El log siempre conserva el detalle completo, aunque el cliente no lo reciba.
    logger.Received(1).LogError(Arg.Is<string>(m => m.Contains("boom")), Arg.Any<Exception>());
  }
}
