using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Logging.Logger;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Infrastructure.Logging.Logger;

public class LoggerDelegatesTests
{
    [Fact]
    public void LogInformation_ShouldUseConfiguredEventIdAndLevel()
    {
        var testLogger = new TestLogger();
        var sut = new LoggerDelegates(testLogger);

        sut.LogInformation("hello info", null);

        testLogger.Entries.Should().ContainSingle();
        testLogger.Entries[0].LogLevel.Should().Be(LogLevel.Information);
        testLogger.Entries[0].EventId.Id.Should().Be(11001);
        testLogger.Entries[0].Message.Should().Contain("hello info");
    }

    [Fact]
    public void LogWarning_ShouldUseConfiguredEventIdAndLevel()
    {
        var testLogger = new TestLogger();
        var sut = new LoggerDelegates(testLogger);

        sut.LogWarning("hello warn", null);

        testLogger.Entries.Should().ContainSingle();
        testLogger.Entries[0].LogLevel.Should().Be(LogLevel.Warning);
        testLogger.Entries[0].EventId.Id.Should().Be(11002);
        testLogger.Entries[0].Message.Should().Contain("hello warn");
    }

    [Fact]
    public void LogError_ShouldUseConfiguredEventIdAndLevel()
    {
        var testLogger = new TestLogger();
        var sut = new LoggerDelegates(testLogger);

        sut.LogError("hello error", new InvalidOperationException("boom"));

        testLogger.Entries.Should().ContainSingle();
        testLogger.Entries[0].LogLevel.Should().Be(LogLevel.Error);
        testLogger.Entries[0].EventId.Id.Should().Be(11003);
        testLogger.Entries[0].Exception.Should().NotBeNull();
    }

    private sealed class TestLogger : ILogger
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
          where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, eventId, formatter(state, exception), exception));
        }
    }

    private sealed record LogEntry(LogLevel LogLevel, EventId EventId, string Message, Exception? Exception);

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
