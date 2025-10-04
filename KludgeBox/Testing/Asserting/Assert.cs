namespace KludgeBox.Testing.Asserting;

/// <summary>
/// A static class containing assertion methods for unit testing.
/// Throws exceptions when conditions are not met.
/// </summary>
public static class Assert
{
    /// <summary>
    /// Asserts that the condition is true.
    /// </summary>
    public static void IsTrue(bool condition, string failMessage = null)
    {
        if (!condition)
        {
            throw new AssertFailException(failMessage ?? "Expected true, but got false");
        }
    }

    /// <summary>
    /// Asserts that the condition is false.
    /// </summary>
    public static void IsFalse(bool condition, string failMessage = null)
    {
        IsTrue(!condition, failMessage ?? "Expected false, but got true");
    }

    /// <summary>
    /// Asserts that the object is null.
    /// </summary>
    public static void IsNull(object obj, string failMessage = null)
    {
        if (obj is not null)
        {
            throw new AssertFailException(failMessage ?? $"Expected null, but got {obj}");
        }
    }

    /// <summary>
    /// Asserts that the object is not null.
    /// </summary>
    public static void IsNotNull(object obj, string failMessage = null)
    {
        if (obj is null)
        {
            throw new AssertFailException(failMessage ?? "Expected not null value, but got null");
        }
    }
    
    /// <summary>
    /// Asserts that two values are equal.
    /// </summary>
    public static void AreEqual<T>(T expected, T actual, string failMessage = null)
    {
        if (!Equals(expected, actual))
        {
            throw new AssertFailException(failMessage ?? $"Expected '{expected}', but got '{actual}'");
        }
    }

    /// <summary>
    /// Asserts that two values are not equal.
    /// </summary>
    public static void AreNotEqual<T>(T notExpected, T actual, string failMessage = null)
    {
        if (Equals(notExpected, actual))
        {
            throw new AssertFailException(failMessage ?? $"Expected value not equal to '{notExpected}', but got '{actual}'");
        }
    }

    /// <summary>
    /// Asserts that value 'a' is greater than value 'b'.
    /// </summary>
    public static void IsGreater<T>(T a, T b, string failMessage = null) where T : IComparable<T>
    {
        if (a.CompareTo(b) <= 0)
        {
            throw new AssertFailException(failMessage ?? $"Expected '{a}' to be greater than '{b}'");
        }
    }

    /// <summary>
    /// Asserts that value 'a' is less than value 'b'.
    /// </summary>
    public static void IsLess<T>(T a, T b, string failMessage = null) where T : IComparable<T>
    {
        if (a.CompareTo(b) >= 0)
        {
            throw new AssertFailException(failMessage ?? $"Expected '{a}' to be less than '{b}'");
        }
    }

    /// <summary>
    /// Asserts that value 'a' is greater than or equal to value 'b'.
    /// </summary>
    public static void IsGreaterOrEqual<T>(T a, T b, string failMessage = null) where T : IComparable<T>
    {
        if (a.CompareTo(b) < 0)
        {
            throw new AssertFailException(failMessage ?? $"Expected '{a}' to be greater than or equal to '{b}'");
        }
    }

    /// <summary>
    /// Asserts that value 'a' is less than or equal to value 'b'.
    /// </summary>
    public static void IsLessOrEqual<T>(T a, T b, string failMessage = null) where T : IComparable<T>
    {
        if (a.CompareTo(b) > 0)
        {
            throw new AssertFailException(failMessage ?? $"Expected '{a}' to be less than or equal to '{b}'");
        }
    }
    
    /// <summary>
    /// Asserts that the given action throws a specific exception type.
    /// </summary>
    public static void Throws<TException>(Action action, string failMessage = null) where TException : Exception
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            if (ex is TException) return;
            throw new AssertFailException(failMessage ?? $"Expected exception of type {typeof(TException).Name}, but got {ex.GetType().Name}");
        }

        throw new AssertFailException(failMessage ?? $"Expected exception of type {typeof(TException).Name}, but no exception was thrown");
    }

    /// <summary>
    /// Asserts that the given action throws any exception.
    /// </summary>
    public static void Throws(Action action, string failMessage = null)
    {
        try
        {
            action();
        }
        catch (Exception)
        {
            return; // success
        }

        throw new AssertFailException(failMessage ?? "Expected exception, but no exception was thrown");
    }

    /// <summary>
    /// Asserts that the given action does not throw any exception.
    /// </summary>
    public static void NotThrows(Action action, string failMessage = null)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            throw new AssertFailException(failMessage ?? $"Expected no exception, but got {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Forces the test to fail with a custom message.
    /// </summary>
    public static void Fail(string failMessage = "Test failed")
    {
        throw new AssertFailException(failMessage);
    }

    /// <summary>
    /// Forces the test to succeed with a custom message.
    /// </summary>
    public static void Success(string message = "Forced success")
    {
        throw new AssertSuccessException(message);
    }

    /// <summary>
    /// Forces a specific result for the test explicitly.
    /// </summary>
    public static void Force(TestResult result, string message = "Forced result")
    {
        throw new AssertResultException(result, message);
    }

    /// <summary>
    /// Skips the test with a specified message.
    /// </summary>
    public static void Skip(string message = "Test skipped from running test method")
    {
        throw new AssertSkipException(message);
    }
}