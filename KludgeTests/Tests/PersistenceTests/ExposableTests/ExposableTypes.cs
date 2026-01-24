using System;
using System.Collections.Generic;
using Godot;
using Persistence;

namespace KludgeTests.Tests.PersistenceTests.ExposableTests;

internal class BasicExposable : IExposable, IEquatable<BasicExposable>
{
    public int SomeIntValue;
    public double SomeDoubleValue;
    public string SomeStringValue;
    public Vector2 SomeVector2Value;

    public BasicExposable(int someIntValue, double someDoubleValue, string someStringValue, Vector2 someVector2Value)
    {
        SomeIntValue = someIntValue;
        SomeDoubleValue = someDoubleValue;
        SomeStringValue = someStringValue;
        SomeVector2Value = someVector2Value;
    }

    public BasicExposable(){}

    public void ExposeData(IPersistenceContainer container)
    {
        container.Expose_Value(ref SomeIntValue, nameof(SomeIntValue));
        container.Expose_Value(ref SomeDoubleValue, nameof(SomeDoubleValue));
        container.Expose_Value(ref SomeStringValue, nameof(SomeStringValue));
        container.Expose_Value(ref SomeVector2Value, nameof(SomeVector2Value));
    }

    public bool Equals(BasicExposable other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return SomeIntValue == other.SomeIntValue && SomeDoubleValue.Equals(other.SomeDoubleValue) && SomeStringValue == other.SomeStringValue && SomeVector2Value.Equals(other.SomeVector2Value);
    }

    public override bool Equals(object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((BasicExposable)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(SomeIntValue, SomeDoubleValue, SomeStringValue, SomeVector2Value);
    }
}

internal class BasicExposableWithListsOfValues : IExposable
{
    public List<string> Strings;
    public List<int> Integers;
    public List<Vector2> Vectors;
    public BasicExposableWithListsOfValues(){}

    public BasicExposableWithListsOfValues(List<string> strings, List<int> integers, List<Vector2> vectors)
    {
        Strings = strings;
        Integers = integers;
        Vectors = vectors;
    }

    public void ExposeData(IPersistenceContainer container)
    {
        container.Expose_List(ref Strings, nameof(Strings));
        container.Expose_List(ref Integers, nameof(Integers));
        container.Expose_List(ref Vectors, nameof(Vectors));
    }
}

internal class BasicExposableWithListOfExposables : IExposable
{
    public List<IExposable> Exposables;
    public BasicExposableWithListOfExposables(){}

    public BasicExposableWithListOfExposables(List<IExposable> exposables)
    {
        Exposables = exposables;
    }

    public void ExposeData(IPersistenceContainer container)
    {
        container.Expose_List(ref Exposables, nameof(Exposables));
    }
}

internal class NestedExposable : IExposable, IEquatable<NestedExposable>
{
    public BasicExposable Nested;
    public int SomeIntValue;
    public double SomeDoubleValue;
    public string SomeStringValue;
    public Vector2 SomeVector2Value;
    
    public NestedExposable(){}

    public NestedExposable(BasicExposable nested, int someIntValue, double someDoubleValue, string someStringValue, Vector2 someVector2Value)
    {
        Nested = nested;
        SomeIntValue = someIntValue;
        SomeDoubleValue = someDoubleValue;
        SomeStringValue = someStringValue;
        SomeVector2Value = someVector2Value;
    }

    public void ExposeData(IPersistenceContainer container)
    {
        container.Expose_Value(ref SomeIntValue, nameof(SomeIntValue));
        container.Expose_Value(ref SomeDoubleValue, nameof(SomeDoubleValue));
        container.Expose_Value(ref SomeStringValue, nameof(SomeStringValue));
        container.Expose_Value(ref SomeVector2Value, nameof(SomeVector2Value));
        container.Expose_Deep(ref Nested, nameof(Nested));
    }

    public bool Equals(NestedExposable other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Equals(Nested, other.Nested) && SomeIntValue == other.SomeIntValue && SomeDoubleValue.Equals(other.SomeDoubleValue) && SomeStringValue == other.SomeStringValue && SomeVector2Value.Equals(other.SomeVector2Value);
    }

    public override bool Equals(object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((NestedExposable)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Nested, SomeIntValue, SomeDoubleValue, SomeStringValue, SomeVector2Value);
    }
}

internal class ReferenceExposable : IRefExposable
{
    private static int _lastId = 0;

    public string SomeStringValue;
    private int _id;

    public ReferenceExposable(string someStringValue) : this()
    {
        SomeStringValue = someStringValue;
    }
    
    private ReferenceExposable()
    {
        _id = _lastId++;
    }
    public void ExposeData(IPersistenceContainer container)
    {
        container.Expose_Value(ref SomeStringValue, nameof(SomeStringValue));
    }

    public string GetUniqueId()
    {
        return $"ReferenceExposable_{_id}";
    }
}

internal class ExposableWithNestedReferences : IExposable
{
    public ReferenceExposable Reference1;
    public ReferenceExposable Reference2;

    public ExposableWithNestedReferences() {}

    public ExposableWithNestedReferences(ReferenceExposable sharedRef)
    {
        Reference1 = sharedRef;
        Reference2 = sharedRef;
    }
    
    public void ExposeData(IPersistenceContainer container)
    {
        container.Expose_Reference(ref Reference1, nameof(Reference1));
        container.Expose_Reference(ref Reference2, nameof(Reference2));
    }
}