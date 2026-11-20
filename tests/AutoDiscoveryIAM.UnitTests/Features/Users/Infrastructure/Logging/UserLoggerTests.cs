using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Logging;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Infrastructure.Logging;

public class UserLoggerTests
{
    [Theory]
    [InlineData(true, LogLevel.Information)]
    [InlineData(false, LogLevel.Error)]
    public void LogUserCreated_ShouldLogExpectedLevel(bool success, LogLevel expectedLevel)
    {
        var testLogger = new TestLogger<UserLogger>();
        var sut = new UserLogger(testLogger);

        sut.LogUserCreated(10, "john", "john@example.com", success);

        testLogger.Entries.Should().ContainSingle();
        testLogger.Entries[0].LogLevel.Should().Be(expectedLevel);
    }

    [Theory]
    [InlineData(true, LogLevel.Information)]
    [InlineData(false, LogLevel.Error)]
    public void LogUserUpdated_ShouldLogExpectedLevel(bool success, LogLevel expectedLevel)
    {
        var testLogger = new TestLogger<UserLogger>();
        var sut = new UserLogger(testLogger);

        sut.LogUserUpdated(10, "john", success);

        testLogger.Entries.Should().ContainSingle();
        testLogger.Entries[0].LogLevel.Should().Be(expectedLevel);
    }

    [Theory]
    [InlineData(true, LogLevel.Information)]
    [InlineData(false, LogLevel.Error)]
    public void LogUserDeleted_ShouldLogExpectedLevel(bool success, LogLevel expectedLevel)
    {
        var testLogger = new TestLogger<UserLogger>();
        var sut = new UserLogger(testLogger);

        sut.LogUserDeleted(10, "john", success);

        testLogger.Entries.Should().ContainSingle();
        testLogger.Entries[0].LogLevel.Should().Be(expectedLevel);
    }

    [Theory]
    [InlineData(true, LogLevel.Information)]
    [InlineData(false, LogLevel.Error)]
    public void LogUserRestored_ShouldLogExpectedLevel(bool success, LogLevel expectedLevel)
    {
        var testLogger = new TestLogger<UserLogger>();
        var sut = new UserLogger(testLogger);

        sut.LogUserRestored(10, "john", success);

        testLogger.Entries.Should().ContainSingle();
        testLogger.Entries[0].LogLevel.Should().Be(expectedLevel);
    }

    [Fact]
    public void LogUserNotFound_ShouldLogWarning()
    {
        var testLogger = new TestLogger<UserLogger>();
        var sut = new UserLogger(testLogger);

        sut.LogUserNotFound(99);

        testLogger.Entries.Should().ContainSingle();
        testLogger.Entries[0].LogLevel.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public void LogUserNotFoundByPublicId_ShouldLogWarning()
    {
        var testLogger = new TestLogger<UserLogger>();
        var sut = new UserLogger(testLogger);

        sut.LogUserNotFoundByPublicId(Guid.NewGuid());

        testLogger.Entries.Should().ContainSingle();
        testLogger.Entries[0].LogLevel.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public void LogUserUpdatedSuccessfully_ShouldLogInformation()
    {
        var testLogger = new TestLogger<UserLogger>();
        var sut = new UserLogger(testLogger);

        sut.LogUserUpdatedSuccessfully(Guid.NewGuid(), "name=alice");

        testLogger.Entries.Should().ContainSingle();
        testLogger.Entries[0].LogLevel.Should().Be(LogLevel.Information);
    }

    [Fact]
    public void LogUsersRetrieved_ShouldLogInformation()
    {
        var testLogger = new TestLogger<UserLogger>();
        var sut = new UserLogger(testLogger);

        sut.LogUsersRetrieved(42);

        testLogger.Entries.Should().ContainSingle();
        testLogger.Entries[0].LogLevel.Should().Be(LogLevel.Information);
    }

    [Fact]
    public void LogUserRetrieved_ShouldLogInformation()
    {
        var testLogger = new TestLogger<UserLogger>();
        var sut = new UserLogger(testLogger);

        sut.LogUserRetrieved(1);

        testLogger.Entries.Should().ContainSingle();
        testLogger.Entries[0].LogLevel.Should().Be(LogLevel.Information);
    }

    [Theory]
    [InlineData(true, LogLevel.Information)]
    [InlineData(false, LogLevel.Warning)]
    public void LogPasswordChanged_ShouldLogExpectedLevel(bool success, LogLevel expectedLevel)
    {
        var testLogger = new TestLogger<UserLogger>();
        var sut = new UserLogger(testLogger);

        sut.LogPasswordChanged(10, success);

        testLogger.Entries.Should().ContainSingle();
        testLogger.Entries[0].LogLevel.Should().Be(expectedLevel);
    }

    [Theory]
    [InlineData(true, LogLevel.Information)]
    [InlineData(false, LogLevel.Warning)]
    public void LogPasswordResetRequested_ShouldLogExpectedLevel(bool success, LogLevel expectedLevel)
    {
        var testLogger = new TestLogger<UserLogger>();
        var sut = new UserLogger(testLogger);

        sut.LogPasswordResetRequested("user@example.com", success);

        testLogger.Entries.Should().ContainSingle();
        testLogger.Entries[0].LogLevel.Should().Be(expectedLevel);
    }

    [Theory]
    [InlineData(true, LogLevel.Information)]
    [InlineData(false, LogLevel.Warning)]
    public void LogPasswordResetConfirmed_ShouldLogExpectedLevel(bool success, LogLevel expectedLevel)
    {
        var testLogger = new TestLogger<UserLogger>();
        var sut = new UserLogger(testLogger);

        sut.LogPasswordResetConfirmed(10, success);

        testLogger.Entries.Should().ContainSingle();
        testLogger.Entries[0].LogLevel.Should().Be(expectedLevel);
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
        }
    }

    private sealed record LogEntry(LogLevel LogLevel, string Message, Exception? Exception);

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
