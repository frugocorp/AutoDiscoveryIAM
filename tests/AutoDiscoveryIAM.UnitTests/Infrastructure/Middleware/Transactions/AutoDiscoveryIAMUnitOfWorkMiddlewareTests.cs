using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Transactions;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Infrastructure.Middleware.Transactions;

public class UnitOfWorkMiddlewareTests
{
  [Fact]
  public async Task InvokeAsync_WhenMethodIsGet_ShouldSkipTransactionAndContinuePipeline()
  {
    var nextCalled = false;
    RequestDelegate next = _ =>
    {
      nextCalled = true;
      return Task.CompletedTask;
    };

    var middleware = new UnitOfWorkMiddleware(next);
    var unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
    var context = new DefaultHttpContext();
    context.Request.Method = HttpMethods.Get;

    await middleware.InvokeAsync(context, unitOfWork);

    nextCalled.Should().BeTrue();
    await unitOfWork.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
    await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    await unitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task InvokeAsync_WhenMethodIsPostAndNoException_ShouldBeginAndCommitTransaction()
  {
    RequestDelegate next = _ => Task.CompletedTask;

    var middleware = new UnitOfWorkMiddleware(next);
    var unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
    var context = new DefaultHttpContext();
    context.Request.Method = HttpMethods.Post;

    await middleware.InvokeAsync(context, unitOfWork);

    await unitOfWork.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
    await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    await unitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task InvokeAsync_WhenMethodIsDeleteAndNextThrows_ShouldRollbackAndRethrow()
  {
    RequestDelegate next = _ => throw new InvalidOperationException("pipeline failed");

    var middleware = new UnitOfWorkMiddleware(next);
    var unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
    var context = new DefaultHttpContext();
    context.Request.Method = HttpMethods.Delete;

    var act = () => middleware.InvokeAsync(context, unitOfWork);

    await ((Func<Task>)(act)).Should().ThrowAsync<InvalidOperationException>();
    await unitOfWork.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
    await unitOfWork.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
    await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
  }
}
