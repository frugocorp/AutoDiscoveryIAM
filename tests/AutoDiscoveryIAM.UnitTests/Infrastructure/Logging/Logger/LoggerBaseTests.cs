using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Logging.Logger;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Infrastructure.Logging.Logger;

public class LoggerBaseTests
{
    [Fact]
    public void LogInformation_ShouldWriteInformationLevel()
    {
        var testLogger = new TestLogger<LoggerBaseTests>();
        var sut = new LoggerBase<LoggerBaseTests>(testLogger);

        sut.LogInformation("info message", null);

        testLogger.Entries.Should().ContainSingle();
        testLogger.Entries[0].LogLevel.Should().Be(LogLevel.Information);
        testLogger.Entries[0].Message.Should().Contain("info message");
    }

    [Fact]
    public void LogWarning_ShouldWriteWarningLevel()
    {
        var testLogger = new TestLogger<LoggerBaseTests>();
        var sut = new LoggerBase<LoggerBaseTests>(testLogger);

        sut.LogWarning("warn message", null);

        testLogger.Entries.Should().ContainSingle();
        testLogger.Entries[0].LogLevel.Should().Be(LogLevel.Warning);
        testLogger.Entries[0].Message.Should().Contain("warn message");
    }

    [Fact]
    public void LogError_ShouldWriteErrorLevel()
    {
        var testLogger = new TestLogger<LoggerBaseTests>();
        var sut = new LoggerBase<LoggerBaseTests>(testLogger);

        sut.LogError("error message", new InvalidOperationException("boom"));

        testLogger.Entries.Should().ContainSingle();
        testLogger.Entries[0].LogLevel.Should().Be(LogLevel.Error);
        testLogger.Entries[0].Message.Should().Contain("error message");
        testLogger.Entries[0].Exception.Should().NotBeNull();
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
