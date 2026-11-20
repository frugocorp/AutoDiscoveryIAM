#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace AwesomeAssertions;

public static class AwesomeAssert
{
    public static void Equal<T>(T expected, T actual)
    {
        actual.Should().Be(expected);
    }

    public static void NotEqual<T>(T expected, T actual)
    {
        actual.Should().NotBe(expected);
    }

    public static void True(bool condition, string porque = "")
    {
        condition.Should().BeTrue(porque);
    }

    public static void False(bool condition, string porque = "")
    {
        condition.Should().BeFalse(porque);
    }

    public static void Null(object? value)
    {
        value.Should().BeNull();
    }

    public static T NotNull<T>([NotNull] T? value)
    {
        if (value is null)
        {
            throw new Xunit.Sdk.XunitException("Se esperaba un valor no nulo.");
        }

        return value!;
    }

    public static void Empty(string value)
    {
        value.Should().BeEmpty();
    }

    public static void Empty<T>(IEnumerable<T> collection)
    {
        collection.Should().BeEmpty();
    }

    public static void NotEmpty(string value)
    {
        value.Should().NotBeEmpty();
    }

    public static void NotEmpty<T>(IEnumerable<T> collection)
    {
        collection.Should().NotBeEmpty();
    }

    public static void Contains(string expectedSubstring, string? actualString)
    {
        actualString.Should().Contain(expectedSubstring);
    }

    public static void Contains(string expectedSubstring, string? actualString, StringComparison comparisonType)
    {
        if (actualString is null)
        {
            throw new Xunit.Sdk.XunitException("Se esperaba un texto no nulo.");
        }

        actualString.Contains(expectedSubstring, comparisonType).Should().BeTrue();
    }

    public static void Contains<T>(IEnumerable<T> collection, Func<T, bool> predicate)
    {
        collection.Should().Contain(item => predicate((T)item));
    }

    public static void Contains<T>(T expected, IEnumerable<T> collection)
    {
        collection.Should().Contain(expected);
    }

    public static void DoesNotContain(string expectedSubstring, string? actualString)
    {
        actualString.Should().NotContain(expectedSubstring);
    }

    public static void DoesNotContain(string expectedSubstring, string? actualString, StringComparison comparisonType)
    {
        if (actualString is null)
        {
            throw new Xunit.Sdk.XunitException("Se esperaba un texto no nulo.");
        }

        actualString.Contains(expectedSubstring, comparisonType).Should().BeFalse();
    }

    public static void DoesNotContain<T>(IEnumerable<T> collection, Func<T, bool> predicate)
    {
        collection.Should().NotContain(item => predicate((T)item));
    }

    public static void DoesNotContain<T>(T expected, IEnumerable<T> collection)
    {
        collection.Should().NotContain(expected);
    }

    public static void StartsWith(string expected, string? actualString)
    {
        actualString.Should().StartWith(expected);
    }

    public static void All<T>(IEnumerable<T> collection, Action<T> assertion)
    {
        collection.Should().AllSatisfy(item => assertion((T)item));
    }

    [SuppressMessage("Naming", "CA1720", Justification = "Paridad con API de xUnit")]
    public static T Single<T>(IEnumerable<T> collection)
    {
        return (T)collection.Should().ContainSingle().Which;
    }

    public static T IsType<T>(object? value)
    {
        return Xunit.Assert.IsType<T>(value);
    }

    public static T IsAssignableFrom<T>(object? value)
    {
        return Xunit.Assert.IsAssignableFrom<T>(value);
    }

    public static void Equivalent(object expected, object actual)
    {
        actual.Should().BeEquivalentTo(expected);
    }

    public static void Fail(string message)
    {
        throw new Xunit.Sdk.XunitException(message);
    }

    public static TException Throws<TException>(Action action)
        where TException : Exception
    {
        return action.Should().Throw<TException>().Subject;
    }

    public static Task<TException> ThrowsAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        return Xunit.Assert.ThrowsAsync<TException>(action);
    }
}
