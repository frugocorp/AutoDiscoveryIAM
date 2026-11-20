using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Logging.Services;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Infrastructure.Logging.Services;

public class LoggerServiceTests
{
    [Fact]
    public void LogInformation_ShouldWriteEntryThroughConcreteService()
    {
        var testLogger = new TestLogger<LoggerService>();
        var sut = new LoggerService(testLogger);

        sut.LogInformation("service info", null);

        testLogger.Entries.Should().ContainSingle();
        testLogger.Entries[0].LogLevel.Should().Be(LogLevel.Information);
        testLogger.Entries[0].Message.Should().Contain("service info");
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
        public void Dispose() { }
    }
}
