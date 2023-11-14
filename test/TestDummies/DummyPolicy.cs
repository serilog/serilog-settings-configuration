using Serilog.Core;
using Serilog.Events;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace TestDummies;

public class DummyPolicy : IDestructuringPolicy
{
    public static DummyPolicy? Current { get; set; }

    public Type[]? Array { get; set; }

    public List<Type>? List { get; set; }

    public CustomCollection<Type>? Custom { get; set; }

    public CustomCollection<string>? CustomStrings { get; set; }

    public Type? Type { get; set; }

    public float Float { get; set; }

    public double Double { get; set; }

    public decimal Decimal { get; set; }

    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, [NotNullWhen(true)] out LogEventPropertyValue? result)
    {
        result = null;
        return false;
    }
}

public class CustomCollection<T> : IEnumerable<T>
{
    private readonly List<T> inner = new List<T>();

    public void Add(T item) => inner.Add(item);

    // wrong signature for collection initializer
    public int Add() => 0;

    // wrong signature for collection initializer
    public void Add(string a, byte b) { }

    public T? First => inner.Count > 0 ? inner[0] : default;

    public IEnumerator<T> GetEnumerator() => inner.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => inner.GetEnumerator();
}
