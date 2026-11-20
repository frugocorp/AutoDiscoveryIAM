#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xunit;

namespace AwesomeAssertions;

public static class AssertionExtensions
{
  public static ValueAssertions<T> Should<T>(this T subject)
  {
    return new ValueAssertions<T>(subject);
  }

  public static ActionAssertions Should(this Action action)
  {
    return new ActionAssertions(action);
  }

  public static FuncAssertions<TResult> Should<TResult>(this Func<TResult> func)
  {
    return new FuncAssertions<TResult>(func);
  }

  public static TaskFuncAssertions Should(this Func<System.Threading.Tasks.Task> func)
  {
    return new TaskFuncAssertions(func);
  }
}

public sealed class ValueAssertions<T>
{
  public ValueAssertions(T subject)
  {
    Subject = subject;
  }

  public T Subject { get; }

  public ValueAssertions<T> And => this;

  public ValueAssertions<T> Be(T expected, string porque = "", params object[] becauseArgs)
  {
    Xunit.Assert.Equal(expected, Subject);
    return this;
  }

  public ValueAssertions<T> NotBe(T expected, string porque = "", params object[] becauseArgs)
  {
    Xunit.Assert.NotEqual(expected, Subject);
    return this;
  }

  public ValueAssertions<T> BeTrue(string porque = "", params object[] becauseArgs)
  {
    Xunit.Assert.True(Subject is bool b && b);
    return this;
  }

  public ValueAssertions<T> BeFalse(string porque = "", params object[] becauseArgs)
  {
    Xunit.Assert.True(Subject is bool b && !b);
    return this;
  }

  public ValueAssertions<T> BeNull(string porque = "", params object[] becauseArgs)
  {
    Xunit.Assert.Null(Subject);
    return this;
  }

  public ValueAssertions<T> NotBeNull(string porque = "", params object[] becauseArgs)
  {
    Xunit.Assert.NotNull(Subject);
    return this;
  }

  public ValueAssertions<T> NotBeNullOrEmpty(string porque = "", params object[] becauseArgs)
  {
    var text = Subject as string;
    Xunit.Assert.False(string.IsNullOrEmpty(text));
    return this;
  }

  public ValueAssertions<T> NotBeNullOrWhiteSpace(string porque = "", params object[] becauseArgs)
  {
    var text = Subject as string;
    Xunit.Assert.False(string.IsNullOrWhiteSpace(text));
    return this;
  }

  public ValueAssertions<T> BeEmpty(string porque = "", params object[] becauseArgs)
  {
    if (Subject is string s)
    {
      Xunit.Assert.Empty(s);
      return this;
    }

    Xunit.Assert.Empty(AsEnumerable());
    return this;
  }

  public ValueAssertions<T> NotBeEmpty(string porque = "", params object[] becauseArgs)
  {
    if (Subject is string s)
    {
      Xunit.Assert.NotEmpty(s);
      return this;
    }

    Xunit.Assert.NotEmpty(AsEnumerable());
    return this;
  }

  public ValueAssertions<T> StartWith(string expected, string porque = "", params object[] becauseArgs)
  {
    Xunit.Assert.StartsWith(expected, Subject as string ?? string.Empty);
    return this;
  }

  public ValueAssertions<T> Contain(string expected, string porque = "", params object[] becauseArgs)
  {
    if (Subject is string s)
    {
      Xunit.Assert.Contains(expected, s);
      return this;
    }

    Xunit.Assert.Contains(AsEnumerable(), item => Equals(item, expected));
    return this;
  }

  public ValueAssertions<T> Contain(object expected, string porque = "", params object[] becauseArgs)
  {
    Xunit.Assert.Contains(AsEnumerable(), item => Equals(item, expected));
    return this;
  }

  public ValueAssertions<T> NotContain(string expected, string porque = "", params object[] becauseArgs)
  {
    if (Subject is string s)
    {
      Xunit.Assert.DoesNotContain(expected, s);
      return this;
    }

    Xunit.Assert.DoesNotContain(AsEnumerable(), item => Equals(item, expected));
    return this;
  }

  public ValueAssertions<T> NotContain(object expected, string porque = "", params object[] becauseArgs)
  {
    Xunit.Assert.DoesNotContain(AsEnumerable(), item => Equals(item, expected));
    return this;
  }

  public ValueAssertions<T> Contain(Func<dynamic, bool> predicate, string porque = "", params object[] becauseArgs)
  {
    Xunit.Assert.Contains(AsEnumerable(), item => predicate((dynamic)item));
    return this;
  }

  public ValueAssertions<T> NotContain(Func<dynamic, bool> predicate, string porque = "", params object[] becauseArgs)
  {
    Xunit.Assert.DoesNotContain(AsEnumerable(), item => predicate((dynamic)item));
    return this;
  }

  public ValueAssertions<T> HaveCount(int expected, string porque = "", params object[] becauseArgs)
  {
    Xunit.Assert.Equal(expected, AsEnumerable().Count);
    return this;
  }

  public ValueAssertions<T> BeGreaterThan(object expected, string porque = "", params object[] becauseArgs)
  {
    Xunit.Assert.True(Compare(Subject, expected) > 0);
    return this;
  }

  public ValueAssertions<T> BeGreaterThanOrEqualTo(object expected, string porque = "", params object[] becauseArgs)
  {
    Xunit.Assert.True(Compare(Subject, expected) >= 0);
    return this;
  }

  public ValueAssertions<T> BeLessThan(object expected, string porque = "", params object[] becauseArgs)
  {
    Xunit.Assert.True(Compare(Subject, expected) < 0);
    return this;
  }

  public ValueAssertions<T> BeAfter(DateTime expected, string porque = "", params object[] becauseArgs)
  {
    var actual = Xunit.Assert.IsType<DateTime>(Subject);
    Xunit.Assert.True(actual > expected);
    return this;
  }

  public ValueAssertions<T> BeCloseTo(DateTime expected, TimeSpan precision, string porque = "", params object[] becauseArgs)
  {
    var actual = Xunit.Assert.IsType<DateTime>(Subject);
    var delta = (actual - expected).Duration();
    Xunit.Assert.True(delta <= precision);
    return this;
  }

  public ValueAssertions<T> BeOneOf(params T[] expected)
  {
    Xunit.Assert.Contains(Subject, expected);
    return this;
  }

  public ValueAssertions<T> ContainKey(object key, string porque = "", params object[] becauseArgs)
  {
    Xunit.Assert.True(Subject is IDictionary dict && dict.Contains(key));
    return this;
  }

  public ValueAssertions<T> NotContainKey(object key, string porque = "", params object[] becauseArgs)
  {
    Xunit.Assert.True(Subject is IDictionary dict && !dict.Contains(key));
    return this;
  }

  public SingleItemAssertion ContainSingle(string porque = "", params object[] becauseArgs)
  {
    var items = AsEnumerable();
    Xunit.Assert.Single(items);
    return new SingleItemAssertion(items[0]);
  }

  public ValueAssertions<T> AllSatisfy(Action<dynamic> assertion, string porque = "", params object[] becauseArgs)
  {
    foreach (var item in AsEnumerable())
    {
      assertion(item);
    }

    return this;
  }

  public ValueAssertions<T> BeEquivalentTo(object expected, string porque = "", params object[] becauseArgs)
  {
    Xunit.Assert.Equivalent(expected, Subject);
    return this;
  }

  public ValueAssertions<T> BeAssignableTo<TExpected>(string porque = "", params object[] becauseArgs)
  {
    Xunit.Assert.IsAssignableFrom<TExpected>(Subject);
    return this;
  }

  public ValueAssertions<T> BeInAscendingOrder(Func<dynamic, IComparable> keySelector, string porque = "", params object[] becauseArgs)
  {
    var keys = new List<IComparable>();
    foreach (var item in AsEnumerable())
    {
      keys.Add(keySelector((dynamic)item));
    }

    for (var i = 1; i < keys.Count; i++)
    {
      Xunit.Assert.True(keys[i - 1].CompareTo(keys[i]) <= 0);
    }

    return this;
  }

  public ValueAssertions<T> NotContainNulls(string porque = "", params object[] becauseArgs)
  {
    Xunit.Assert.DoesNotContain(AsEnumerable(), item => item is null);
    return this;
  }

  private List<object> AsEnumerable()
  {
    Xunit.Assert.NotNull(Subject);
    Xunit.Assert.True(Subject is IEnumerable, "El objeto debe ser enumerable para esta aserción.");
    var result = new List<object>();
    foreach (var item in (IEnumerable)Subject)
    {
      result.Add(item);
    }

    return result;
  }

  private static int Compare(object left, object right)
  {
    Xunit.Assert.NotNull(left);
    if (left is IComparable comparable)
    {
      try
      {
        return comparable.CompareTo(right);
      }
#pragma warning disable CA1031
      catch (ArgumentException)
#pragma warning restore CA1031
      {
      }

      if (right is IConvertible)
      {
        var converted = Convert.ChangeType(right, left.GetType(), CultureInfo.InvariantCulture);
        return comparable.CompareTo(converted);
      }
    }

    Xunit.Assert.Fail($"No se puede comparar {left} ({left.GetType().Name}) with {right} ({right?.GetType().Name})");
    return 0;
  }
}

public sealed class SingleItemAssertion
{
  public SingleItemAssertion(object which)
  {
    Which = which;
  }

  public object Which { get; }
}

public sealed class ActionAssertions
{
  private readonly Action _action;

  public ActionAssertions(Action action)
  {
    _action = action;
  }

  public ExceptionAssertions<TException> Throw<TException>(string porque = "", params object[] becauseArgs)
      where TException : Exception
  {
    var ex = Xunit.Assert.Throws<TException>(_action);
    return new ExceptionAssertions<TException>(ex);
  }
}

public sealed class FuncAssertions<TResult>
{
  private readonly Func<TResult> _func;

  public FuncAssertions(Func<TResult> func)
  {
    _func = func;
  }

  public ExceptionAssertions<TException> Throw<TException>(string porque = "", params object[] becauseArgs)
      where TException : Exception
  {
    var ex = Xunit.Record.Exception(() => _func());
    var typed = Xunit.Assert.IsType<TException>(ex);
    return new ExceptionAssertions<TException>(typed);
  }
}

public sealed class TaskFuncAssertions
{
  private readonly Func<System.Threading.Tasks.Task> _func;

  public TaskFuncAssertions(Func<System.Threading.Tasks.Task> func)
  {
    _func = func;
  }

  public async System.Threading.Tasks.Task<ExceptionAssertions<TException>> ThrowAsync<TException>(string porque = "", params object[] becauseArgs)
      where TException : Exception
  {
    var ex = await Xunit.Assert.ThrowsAsync<TException>(_func);
    return new ExceptionAssertions<TException>(ex);
  }
}

public sealed class ExceptionAssertions<TException>
    where TException : Exception
{
  private static readonly char[] WildcardSeparator = new[] { '*' };

  public ExceptionAssertions(TException exception)
  {
    Subject = exception;
  }

  public TException Subject { get; }

  public ExceptionAssertions<TException> WithMessage(string wildcardPattern, string porque = "", params object[] becauseArgs)
  {
    var message = Subject.Message ?? string.Empty;
    var parts = wildcardPattern.Split(WildcardSeparator, StringSplitOptions.RemoveEmptyEntries);
    var searchIn = NormalizeText(message);
    var pos = 0;
    foreach (var part in parts)
    {
      var needle = NormalizeText(part);
      var found = searchIn.IndexOf(needle, pos, StringComparison.Ordinal);
      Xunit.Assert.True(found >= 0, $"Se esperaba que el mensaje de excepción\n    \"{message}\"\n    coincidiera con el patrón\n    \"{wildcardPattern}\"");
      pos = found + needle.Length;
    }

    return this;
  }

  private static string NormalizeText(string value)
  {
    if (string.IsNullOrEmpty(value)) return string.Empty;
    var decomposed = value.Normalize(NormalizationForm.FormD);
    var sb = new StringBuilder();
    foreach (var c in decomposed)
    {
      if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
      {
        sb.Append(c);
      }
    }

    return sb.ToString().ToLowerInvariant();
  }
}
