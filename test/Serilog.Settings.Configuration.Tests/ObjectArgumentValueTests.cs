﻿using System.Collections;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using Serilog.Settings.Configuration.Tests.Support;
using TestDummies;
using TestDummies.Console;
// ReSharper disable NotDisposedResourceIsReturned
// ReSharper disable UnusedAutoPropertyAccessor.Local

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedType.Local

namespace Serilog.Settings.Configuration.Tests;

public class ObjectArgumentValueTests
{
    static T AssertConvertsToType<T>(ObjectArgumentValue value, ResolutionContext? resolutionContext = null)
    {
        return Assert.IsType<T>(value.ConvertTo(typeof(T), resolutionContext ?? new()));
    }

    [Fact]
    public void ConvertsToIConfigurationSection()
    {
        // language=json
        const string json = """
            {
                "section": {}
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "section");
        var value = new ObjectArgumentValue(section, []);

        var actual = value.ConvertTo(typeof(IConfigurationSection), new());

        Assert.Same(section, actual);
    }

    [Fact]
    public void ConvertsToLoggerConfigurationCallback()
    {
        // language=json
        const string json = """
            {
                "callback": {
                    "WriteTo": [{
                        "Name": "DummyRollingFile",
                        "Args": {"pathFormat" : "C:\\"}
                    }],
                    "Enrich": ["WithDummyThreadId"]
                }
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "callback");
        var value = new ObjectArgumentValue(section, [typeof(DummyRollingFileSink).Assembly]);

        var configure = AssertConvertsToType<Action<LoggerConfiguration>>(value);

        var config = new LoggerConfiguration();
        configure(config);
        var log = config.CreateLogger();
        DummyRollingFileSink.Emitted.Clear();

        log.Write(Some.InformationEvent());

        var evt = Assert.Single(DummyRollingFileSink.Emitted);
        Assert.True(evt.Properties.ContainsKey("ThreadId"), "Event should have enriched property ThreadId");
    }

    [Fact]
    public void ConvertsToLoggerSinkConfigurationCallback()
    {
        // language=json
        const string json = """
            {
                "WriteTo": [{
                    "Name": "Dummy",
                    "Args": {
                        "wrappedSinkAction" : [{ "Name": "DummyConsole", "Args": {} }]
                    }
                }]
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "WriteTo");
        var value = new ObjectArgumentValue(section, [typeof(DummyConfigurationSink).Assembly]);

        var configureSinks = AssertConvertsToType<Action<LoggerSinkConfiguration>>(value);

        var config = new LoggerConfiguration();
        configureSinks(config.WriteTo);
        var log = config.CreateLogger();
        DummyConsoleSink.Emitted.Clear();
        DummyWrappingSink.Emitted.Clear();

        log.Write(Some.InformationEvent());

        Assert.Single(DummyWrappingSink.Emitted);
        Assert.Single(DummyConsoleSink.Emitted);
    }

    [Fact]
    public void ConvertsToLoggerEnrichmentConfiguration()
    {
        // language=json
        const string json = """
            {
                "Enrich": [{
                    "Name": "AtLevel",
                    "Args": {
                        "enrichFromLevel": "Warning",
                        "configureEnricher": [ "WithDummyThreadId" ]
                    }
                }]
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "Enrich");
        var value = new ObjectArgumentValue(section, [typeof(LoggerEnrichmentConfiguration).Assembly, typeof(DummyThreadIdEnricher).Assembly]);

        var configureEnrichment = AssertConvertsToType<Action<LoggerEnrichmentConfiguration>>(value);

        var config = new LoggerConfiguration();
        config.WriteTo.DummyRollingFile("");
        configureEnrichment(config.Enrich);
        var log = config.CreateLogger();

        DummyRollingFileSink.Emitted.Clear();

        log.Write(Some.InformationEvent());
        log.Write(Some.WarningEvent());

        Assert.Collection(DummyRollingFileSink.Emitted,
            info => Assert.False(info.Properties.ContainsKey("ThreadId"), "Information event or lower should not have enriched property ThreadId"),
            warn => Assert.True(warn.Properties.ContainsKey("ThreadId"), "Warning event or higher should have enriched property ThreadId"));
    }

    [Fact]
    public void ConvertToUnrecognizedConfigurationCallbackThrows()
    {
        // language=json
        const string json = """
            {
                "configure": {}
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "configure");
        var value = new ObjectArgumentValue(section, []);

        var ex = Assert.Throws<ArgumentException>(() => value.ConvertTo(typeof(Action<ConfigurationReaderOptions>), new()));

        Assert.Equal("Configuration resolution for `Action<ConfigurationReaderOptions>` parameter type at the path `configure` is not implemented.", ex.Message);
    }

    [Fact]
    public void ConvertsToEnumArrayUsingStringArgumentValueForElements()
    {
        // language=json
        const string json = """
            {
                "Array": [ "Information", 3, null ]
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "Array");
        var value = new ObjectArgumentValue(section, []);

        var array = AssertConvertsToType<LogEventLevel?[]>(value);

        Assert.Equal([LogEventLevel.Information, LogEventLevel.Warning, null], array);
    }

    [Fact]
    public void ConvertsToArrayOfArraysPassingContext()
    {
        var formatProvider = new NumberFormatInfo
        {
            NumberDecimalSeparator = ",",
            NumberGroupSeparator = ".",
            NumberGroupSizes = [3],
        };

        // language=json
        const string json = """
            {
                "Array": [ [ 1, 2 ], [ 3, 4 ], [ "1.234,56" ] ]
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "Array");
        var value = new ObjectArgumentValue(section, []);

        var array = AssertConvertsToType<decimal[][]>(value, new(readerOptions: new() { FormatProvider = formatProvider }));

        Assert.Equal([[1, 2], [3, 4], [1_234.56M]], array);
    }

    [Fact]
    public void ConvertsToArrayRecursingObjectArgumentValuePassingAssemblies()
    {
        // language=json
        const string json = """
            {
                "Array": [{ "WriteTo": [{ "Name": "DummyConsole", "Args": {} }] }]
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "Array");
        var value = new ObjectArgumentValue(section, [typeof(DummyConsoleSink).Assembly]);

        var configureCalls = AssertConvertsToType<Action<LoggerConfiguration>[]>(value);

        var configure = Assert.Single(configureCalls);
        var config = new LoggerConfiguration();
        configure(config);
        var log = config.CreateLogger();
        DummyConsoleSink.Emitted.Clear();

        log.Write(Some.InformationEvent());

        Assert.Single(DummyConsoleSink.Emitted);
    }

    [Fact]
    public void ConvertsToArrayWithDifferentImplementations()
    {
        // language=json
        const string json = """
            {
                "Array": [
                    "Serilog.Settings.Configuration.Tests.Support.ConcreteImpl::Instance, Serilog.Settings.Configuration.Tests",
                    "Serilog.Settings.Configuration.Tests.ObjectArgumentValueTests+PrivateImplWithPublicCtor, Serilog.Settings.Configuration.Tests"
                ]
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "Array");
        var value = new ObjectArgumentValue(section, []);

        var array = AssertConvertsToType<AnAbstractClass[]>(value);

        Assert.Collection(array,
            first => Assert.IsType<ConcreteImpl>(first),
            second => Assert.IsType<PrivateImplWithPublicCtor>(second));
    }

    [Fact]
    public void ConvertsToContainerUsingStringArgumentValueForElements()
    {
        // language=json
        const string json = """
            {
                "List": [ "Information", 3, null ]
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "List");
        var value = new ObjectArgumentValue(section, []);

        var list = AssertConvertsToType<List<LogEventLevel?>>(value);

        Assert.Equal([LogEventLevel.Information, LogEventLevel.Warning, null], list);
    }

    [Fact]
    public void ConvertsToNestedContainerPassingContext()
    {
        var formatProvider = new NumberFormatInfo
        {
            NumberDecimalSeparator = ",",
            NumberGroupSeparator = ".",
            NumberGroupSizes = [3],
        };

        // language=json
        const string json = """
            {
              "List": [ [ 1, 2 ], [ 3, 4 ], [ "1.234,56" ] ]
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "List");
        var value = new ObjectArgumentValue(section, []);

        var array = AssertConvertsToType<List<List<decimal>>>(value, new(readerOptions: new() { FormatProvider = formatProvider }));

        Assert.Equal([[1, 2], [3, 4], [1_234.56M]], array);
    }

    [Fact]
    public void ConvertsToContainerRecursingObjectArgumentValuePassingAssemblies()
    {
        // language=json
        const string json = """
            {
                "List": [{ "WriteTo": [{ "Name": "DummyConsole", "Args": {} }] }]
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "List");
        var value = new ObjectArgumentValue(section, [typeof(DummyConsoleSink).Assembly]);

        var configureCalls = AssertConvertsToType<List<Action<LoggerConfiguration>>>(value);

        var configure = Assert.Single(configureCalls);
        var config = new LoggerConfiguration();
        configure(config);
        var log = config.CreateLogger();
        DummyConsoleSink.Emitted.Clear();

        log.Write(Some.InformationEvent());

        Assert.Single(DummyConsoleSink.Emitted);
    }

    [Fact]
    public void ConvertsToListWithDifferentImplementations()
    {
        // language=json
        const string json = """
            {
                "List": [
                    "Serilog.Settings.Configuration.Tests.Support.ConcreteImpl::Instance, Serilog.Settings.Configuration.Tests",
                    "Serilog.Settings.Configuration.Tests.ObjectArgumentValueTests+PrivateImplWithPublicCtor, Serilog.Settings.Configuration.Tests"
                ]
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "List");
        var value = new ObjectArgumentValue(section, []);

        var list = AssertConvertsToType<List<AnAbstractClass>>(value);

        Assert.Collection(list,
            first => Assert.IsType<ConcreteImpl>(first),
            second => Assert.IsType<PrivateImplWithPublicCtor>(second));
    }

    class UnsupportedContainer : IEnumerable<string>
    {
        public IEnumerator<string> GetEnumerator()
        {
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    [Fact]
    public void ConvertToUnsupportedContainerWillBeCreatedButWillRemainEmpty()
    {
        // language=json
        const string json = """
            {
                "List": ["a", "b"]
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "List");
        var value = new ObjectArgumentValue(section, []);

        var unsupported = AssertConvertsToType<UnsupportedContainer>(value);

        Assert.Empty(unsupported);
    }

    [Theory]
    [InlineData(typeof(IEnumerable<int>))]
    [InlineData(typeof(ICollection<int>))]
    [InlineData(typeof(IReadOnlyCollection<int>))]
    [InlineData(typeof(IList<int>))]
    [InlineData(typeof(IReadOnlyList<int>))]
    [InlineData(typeof(List<int>))]
    public void ConvertToContainerUsingList(Type containerType)
    {
        // language=json
        const string json = """
            {
                "Container": [ 1, 1 ]
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "Container");
        var value = new ObjectArgumentValue(section, []);

        var container = value.ConvertTo(containerType, new());

        var list = Assert.IsType<List<int>>(container);
        Assert.Equal([1, 1], list);
    }

    [Theory]
    [InlineData(typeof(ISet<int>))]
#if NET5_0_OR_GREATER
    [InlineData(typeof(IReadOnlySet<int>))]
#endif
    [InlineData(typeof(HashSet<int>))]
    public void ConvertsToContainerUsingHashSet(Type containerType)
    {
        // language=json
        const string json = """
            {
                "Container": [ 1, 1, 2, 2 ]
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "Container");
        var value = new ObjectArgumentValue(section, []);

        var container = value.ConvertTo(containerType, new());

        var set = Assert.IsType<HashSet<int>>(container);
        Assert.Equal([1, 2], set);
    }

    [Fact]
    public void ConvertsToForcedHashSetImplementationWithCustomComparer()
    {
        // In .Net Framework HashSet<T> is not part of mscorlib, but inside System.Core
        // As a result the type string "System.Collections.Generic.HashSet`1[[System.String]]" will fail
        // Using AssemblyQualifiedName to automatically switch to the correct type string, depending on framework

        // language=json
        var json = $$"""
            {
                "Container":
                {
                    "type": "{{typeof(HashSet<string>).AssemblyQualifiedName}}",
                    "collection": [
                        "a",
                        "A",
                        "b",
                        "b"
                    ],
                    "comparer": "System.StringComparer::OrdinalIgnoreCase"
                }
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "Container");
        var value = new ObjectArgumentValue(section, []);

        var container = value.ConvertTo(typeof(IEnumerable<string>), new());

        var set = Assert.IsType<HashSet<string>>(container);
        Assert.Equal(["a", "b"], set);
    }

    [Theory]
    [InlineData(typeof(IEnumerable<KeyValuePair<string, int>>))]
    [InlineData(typeof(ICollection<KeyValuePair<string, int>>))]
    [InlineData(typeof(IReadOnlyCollection<KeyValuePair<string, int>>))]
    [InlineData(typeof(IDictionary<string, int>))]
    [InlineData(typeof(IReadOnlyDictionary<string, int>))]
    [InlineData(typeof(Dictionary<string, int>))]
    public void ConvertsToContainerUsingDictionary(Type containerType)
    {
        // language=json
        const string json = """
            {
                "Container": {
                    "a": 1,
                    "b": 2
                }
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "Container");
        var value = new ObjectArgumentValue(section, []);

        var container = value.ConvertTo(containerType, new());

        var dictionary = Assert.IsType<Dictionary<string, int>>(container);
        Assert.Equal(new Dictionary<string, int> { { "a", 1 }, { "b", 2 } }, dictionary);
    }

    [Theory]
    [InlineData(typeof(IEnumerable<KeyValuePair<int, int>>))]
    [InlineData(typeof(ICollection<KeyValuePair<int, int>>))]
    [InlineData(typeof(IReadOnlyCollection<KeyValuePair<int, int>>))]
    [InlineData(typeof(IDictionary<int, int>))]
    [InlineData(typeof(IReadOnlyDictionary<int, int>))]
    [InlineData(typeof(Dictionary<int, int>))]
    public void ConvertsToContainerUsingDictionaryUsingStringArgumentValueToConvertKey(Type containerType)
    {
        // language=json
        const string json = """
            {
                "Container": {
                    "1": 2,
                    "3": 4
                }
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "Container");
        var value = new ObjectArgumentValue(section, []);

        var container = value.ConvertTo(containerType, new());

        var dictionary = Assert.IsType<Dictionary<int, int>>(container);
        Assert.Equal(new Dictionary<int, int> { { 1, 2 }, { 3, 4 } }, dictionary);
    }

    class DictionaryWithoutPublicDefaultConstructor : IDictionary<string, int>
    {
        readonly IDictionary<string, int> backing;

        public int this[string key] { get => backing[key]; set => backing[key] = value; }

        public ICollection<string> Keys => backing.Keys;

        public ICollection<int> Values => backing.Values;

        public int Count => backing.Count;

        public bool IsReadOnly => backing.IsReadOnly;

        // Normally there would be a default constructor here, like: public DictionaryWithoutPublicDefaultConstructor() {}
        public DictionaryWithoutPublicDefaultConstructor(IDictionary<string, int> values) { backing = values; }

        public void Add(string key, int value)
        {
            backing.Add(key, value);
        }

        public void Add(KeyValuePair<string, int> item)
        {
            backing.Add(item);
        }

        public void Clear()
        {
            backing.Clear();
        }

        public bool Contains(KeyValuePair<string, int> item)
        {
            return backing.Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return backing.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, int>[] array, int arrayIndex)
        {
            backing.CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, int>> GetEnumerator()
        {
            return backing.GetEnumerator();
        }

        public bool Remove(string key)
        {
            return backing.Remove(key);
        }

        public bool Remove(KeyValuePair<string, int> item)
        {
            return backing.Remove(item);
        }

        public bool TryGetValue(string key, out int value)
        {
            return backing.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)backing).GetEnumerator();
        }
    }

    [Fact]
    public void ConvertsToContainerUsingDictionaryWithoutPublicDefaultConstructor()
    {
        // language=json
        const string json = """
            {
                "Container": {
                    "values":
                    {
                        "a": 1,
                        "b": 2
                    }
                }
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "Container");
        var value = new ObjectArgumentValue(section, []);

        var dictionary = AssertConvertsToType<DictionaryWithoutPublicDefaultConstructor>(value);

        Assert.Equal(new Dictionary<string, int> { { "a", 1 }, { "b", 2 } }, dictionary);
    }

    abstract class CustomAbstractDictionary : IDictionary<string, int>
    {
        public abstract int this[string key] { get; set; }

        public abstract ICollection<string> Keys { get; }
        public abstract ICollection<int> Values { get; }
        public abstract int Count { get; }
        public abstract bool IsReadOnly { get; }

        public abstract void Add(string key, int value);
        public abstract void Add(KeyValuePair<string, int> item);
        public abstract void Clear();
        public abstract bool Contains(KeyValuePair<string, int> item);
        public abstract bool ContainsKey(string key);
        public abstract void CopyTo(KeyValuePair<string, int>[] array, int arrayIndex);
        public abstract IEnumerator<KeyValuePair<string, int>> GetEnumerator();
        public abstract bool Remove(string key);
        public abstract bool Remove(KeyValuePair<string, int> item);
        public abstract bool TryGetValue(string key, out int value);

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public void ConvertToCustomAbstractDictionaryThrows()
    {
        // language=json
        const string json = """
            {
                "Container": {
                    "a": 1,
                    "b": 2
                }
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "Container");
        var value = new ObjectArgumentValue(section, []);

        Assert.Throws<InvalidOperationException>(() => value.ConvertTo(typeof(CustomAbstractDictionary), new()));
    }

    class CustomReadOnlyDictionary : IReadOnlyDictionary<string, int>
    {
        public int this[string key] => throw new NotImplementedException();

        public IEnumerable<string> Keys => throw new NotImplementedException();

        public IEnumerable<int> Values => throw new NotImplementedException();

        public int Count => throw new NotImplementedException();

        public bool ContainsKey(string key)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<string, int>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(string key, out int value)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public void ConvertToCustomReadOnlyDictionaryCreatesEmpty()
    {
        // language=json
        const string json = """
            {
                "Container": {
                    "a": 1,
                    "b": 2
                }
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "Container");
        var value = new ObjectArgumentValue(section, []);

        AssertConvertsToType<CustomReadOnlyDictionary>(value);
    }

    class PrivateImplWithPublicCtor : AnAbstractClass, IAmAnInterface;

    [Theory]
    [InlineData(typeof(AbstractClass), typeof(ConcreteClass))]
    [InlineData(typeof(IAmAnInterface), typeof(PrivateImplWithPublicCtor))]
    [InlineData(typeof(AnAbstractClass), typeof(PrivateImplWithPublicCtor))]
    [InlineData(typeof(AConcreteClass), typeof(ConcreteImplOfConcreteClass))]
    public void ConvertsToExplicitType(Type targetType, Type expectedType)
    {
        // language=json
        var json = $$"""
            {
                "Ctor": { "type": "{{expectedType.AssemblyQualifiedName}}"}
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "Ctor");
        var value = new ObjectArgumentValue(section, []);

        var result = value.ConvertTo(targetType, new());

        Assert.IsType(expectedType, result);
    }

    class WithTypeArgumentClassCtor : AnAbstractClass
    {
        public string Type { get; }

        public WithTypeArgumentClassCtor(string type) { Type = type; }
    }

    [Fact]
    public void ConvertsToExplicitTypeUsingTypeAsConstructorArgument()
    {
        // language=json
        const string json = """
            {
                "Ctor": {
                    "$type": "Serilog.Settings.Configuration.Tests.ObjectArgumentValueTests+WithTypeArgumentClassCtor, Serilog.Settings.Configuration.Tests",
                    "type": "Serilog.Settings.Configuration.Tests.ObjectArgumentValueTests+PrivateImplWithPublicCtor, Serilog.Settings.Configuration.Tests"
                }
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "Ctor");
        var value = new ObjectArgumentValue(section, []);

        var result = value.ConvertTo(typeof(AnAbstractClass), new());

        var actual = Assert.IsType<WithTypeArgumentClassCtor>(result);
        Assert.Equal("Serilog.Settings.Configuration.Tests.ObjectArgumentValueTests+PrivateImplWithPublicCtor, Serilog.Settings.Configuration.Tests", actual.Type);
    }

    class WithOverloads : IAmAnInterface
    {
        public int A { get; }
        public TimeSpan B { get; }
        public Uri? C { get; }
        public string? D { get; }

        public WithOverloads(int a, TimeSpan b, Uri c)
        {
            A = a;
            B = b;
            C = c;
        }

        public WithOverloads(int a, TimeSpan b, Uri c, string d = "d")
        {
            A = a;
            B = b;
            C = c;
            D = d;
        }
    }

    [Theory]
    [InlineData("", null)]
    [InlineData(",\"d\": \"DValue\"", "DValue")]
    public void ConvertsToExplicitTypePickingConstructorOverloadWithMostMatchingArguments(string dJson, string? d)
    {
        var json = $$"""
            {
                "Ctor": {
                    "type": "Serilog.Settings.Configuration.Tests.ObjectArgumentValueTests+WithOverloads, Serilog.Settings.Configuration.Tests",
                    "a": 1,
                    "b": "23:59:59",
                    "c": "https://example.com/"
                    {{dJson}}
                }
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "Ctor");
        var value = new ObjectArgumentValue(section, []);

        var result = value.ConvertTo(typeof(IAmAnInterface), new());

        var actual = Assert.IsType<WithOverloads>(result);
        Assert.Equal(1, actual.A);
        Assert.Equal(new TimeSpan(23, 59, 59), actual.B);
        Assert.Equal(new Uri("https://example.com/"), actual.C);
        Assert.Equal(d, actual.D);
    }

    [Fact]
    public void ConvertsToExplicitTypeMatchingArgumentsCaseInsensitively()
    {
        // language=json
        const string json = """
            {
                "Ctor": {
                    "type": "Serilog.Settings.Configuration.Tests.ObjectArgumentValueTests+WithOverloads, Serilog.Settings.Configuration.Tests",
                    "A": 1,
                    "B": "23:59:59",
                    "C": "https://example.com/"
                }
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "Ctor");
        var value = new ObjectArgumentValue(section, []);

        var result = value.ConvertTo(typeof(IAmAnInterface), new());

        var actual = Assert.IsType<WithOverloads>(result);
        Assert.Equal(1, actual.A);
        Assert.Equal(new TimeSpan(23, 59, 59), actual.B);
        Assert.Equal(new Uri("https://example.com/"), actual.C);
    }

    class WithSimilarOverloads : IAmAnInterface
    {
        public object A { get; }
        public object B { get; }
        public object C { get; }
        public int D { get; }

        public WithSimilarOverloads(int a, int b, int c, int d = 1) { A = a; B = b; C = c; D = d; }
        public WithSimilarOverloads(int a, string b, string c) { A = a; B = b; C = c; D = 2; }
        public WithSimilarOverloads(string a, string b, string c) { A = a; B = b; C = c; D = 3; }
    }

    [Fact]
    public void ConvertToExplicitTypePickingConstructorOverloadWithMostStrings()
    {
        // language=json
        const string json = """
            {
                "Ctor": {
                    "type": "Serilog.Settings.Configuration.Tests.ObjectArgumentValueTests+WithSimilarOverloads, Serilog.Settings.Configuration.Tests",
                    "a": 1,
                    "b": 2,
                    "c": 3
                }
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "Ctor");
        var value = new ObjectArgumentValue(section, []);

        var result = value.ConvertTo(typeof(IAmAnInterface), new());
        var actual = Assert.IsType<WithSimilarOverloads>(result);

        Assert.Equal("1", actual.A);
        Assert.Equal("2", actual.B);
        Assert.Equal("3", actual.C);
        Assert.Equal(3, actual.D);
    }

#if NET7_0_OR_GREATER
    class OnlyDifferentTypeOverloads : IAmAnInterface
    {
        public object Value { get; }

        public OnlyDifferentTypeOverloads(int value) { Value = value; }
        public OnlyDifferentTypeOverloads(long value) { Value = value; }
    }

    // Is only guaranteed to work when Type.GetConstructors returns constructors in a deterministic order
    // This is only the case since .Net 7
    [Fact]
    public void ConvertToExplicitTypePickingFirstMatchWhenOtherwiseAmbiguous()
    {
        // language=json
        const string json = """
            {
                "Ctor": {
                    "type": "Serilog.Settings.Configuration.Tests.ObjectArgumentValueTests+OnlyDifferentTypeOverloads, Serilog.Settings.Configuration.Tests",
                    "value": 123
                }
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "Ctor");
        var value = new ObjectArgumentValue(section, []);

        var result = value.ConvertTo(typeof(IAmAnInterface), new());
        var actual = Assert.IsType<OnlyDifferentTypeOverloads>(result);

        Assert.Equal(123, actual.Value);
    }
#endif

    class WithDefaults : IAmAnInterface
    {
        public int A { get; }
        public int B { get; }
        public int C { get; }

        public WithDefaults(int a, int b = 2, int c = 3)
        {
            A = a;
            B = b;
            C = c;
        }
    }

    [Theory]
    [InlineData("", 2, 3)]
    [InlineData(",\"b\": 5", 5, 3)]
    [InlineData(",\"c\": 6", 2, 6)]
    [InlineData(",\"b\": 7, \"c\": 8", 7, 8)]
    public void ConvertsToExplicitTypeFillingInDefaultsInConstructor(string jsonPart, int b, int c)
    {
        var json = $$"""
            {
                "Ctor": {
                    "type": "Serilog.Settings.Configuration.Tests.ObjectArgumentValueTests+WithDefaults, Serilog.Settings.Configuration.Tests",
                    "a": 1
                    {{jsonPart}}
                }
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "Ctor");
        var value = new ObjectArgumentValue(section, []);

        var result = value.ConvertTo(typeof(IAmAnInterface), new());

        var actual = Assert.IsType<WithDefaults>(result);
        Assert.Equal(1, actual.A);
        Assert.Equal(b, actual.B);
        Assert.Equal(c, actual.C);
    }

    class WithParamsArray : IAmAnInterface
    {
        public IReadOnlyList<int> Values { get; }

        public WithParamsArray(params int[] values) { Values = values; }
    }

    [Fact]
    public void ConvertsToExplicitTypeWithParamsConstructorArgument()
    {
        // language=json
        const string json = """
            {
                "Ctor": {
                    "type": "Serilog.Settings.Configuration.Tests.ObjectArgumentValueTests+WithParamsArray, Serilog.Settings.Configuration.Tests",
                    "values": [1, 2, 3]
                }
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "Ctor");
        var value = new ObjectArgumentValue(section, []);

        var result = value.ConvertTo(typeof(IAmAnInterface), new());

        var actual = Assert.IsType<WithParamsArray>(result);
        Assert.Equal([1, 2, 3], actual.Values);
    }

    [Theory]
    [InlineData(typeof(IEnumerable<int>))]
    [InlineData(typeof(ICollection<int>))]
    [InlineData(typeof(IReadOnlyCollection<int>))]
    [InlineData(typeof(IList<int>))]
    [InlineData(typeof(IReadOnlyList<int>))]
    [InlineData(typeof(List<int>))]
    public void ConvertsToExplicitTypeWithContainerConstructorArgument(Type containerType)
    {
        var expectedType = typeof(GenericClass<>).MakeGenericType(containerType);
        var valueProp = expectedType.GetProperty(nameof(GenericClass<object>.Value));

        // language=json
        var json = $$"""
            {
                "Ctor": {
                    "type": "Serilog.Settings.Configuration.Tests.Support.GenericClass`1[[{{containerType.AssemblyQualifiedName}}]], Serilog.Settings.Configuration.Tests",
                    "value": [1, 2, 3]
                }
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "Ctor");
        var value = new ObjectArgumentValue(section, []);

        var result = value.ConvertTo(typeof(IAmAnInterface), new());

        Assert.IsType(expectedType, result);
        var list = Assert.IsType<List<int>>(valueProp?.GetValue(result));
        Assert.Equal([1, 2, 3], list);
    }

    [Theory]
    [InlineData(typeof(ISet<int>))]
#if NET5_0_OR_GREATER
    [InlineData(typeof(IReadOnlySet<int>))]
#endif
    [InlineData(typeof(HashSet<int>))]
    public void ConvertToExplicitTypeWithSetConstructorArgument(Type containerType)
    {
        var expectedType = typeof(GenericClass<>).MakeGenericType(containerType);
        var valueProp = expectedType.GetProperty(nameof(GenericClass<object>.Value));

        // language=json
        var json = $$"""
            {
                "Ctor": {
                    "type": "Serilog.Settings.Configuration.Tests.Support.GenericClass`1[[{{containerType.AssemblyQualifiedName}}]], Serilog.Settings.Configuration.Tests",
                    "value": [ 1, 1, 2, 2 ]
                }
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "Ctor");
        var value = new ObjectArgumentValue(section, []);

        var result = value.ConvertTo(typeof(IAmAnInterface), new());

        Assert.IsType(expectedType, result);
        var set = Assert.IsType<HashSet<int>>(valueProp?.GetValue(result));
        Assert.Equal([1, 2], set);
    }


    [Theory]
    [InlineData(typeof(IEnumerable<KeyValuePair<string, int>>))]
    [InlineData(typeof(ICollection<KeyValuePair<string, int>>))]
    [InlineData(typeof(IReadOnlyCollection<KeyValuePair<string, int>>))]
    [InlineData(typeof(IDictionary<string, int>))]
    [InlineData(typeof(IReadOnlyDictionary<string, int>))]
    [InlineData(typeof(Dictionary<string, int>))]
    public void ConvertsToExplicitTypeWithDictionaryConstructorArgument(Type containerType)
    {
        var expectedType = typeof(GenericClass<>).MakeGenericType(containerType);
        var valueProp = expectedType.GetProperty(nameof(GenericClass<object>.Value));

        // language=json
        var json = $$"""
            {
                "Ctor": {
                    "type": "Serilog.Settings.Configuration.Tests.Support.GenericClass`1[[{{containerType.AssemblyQualifiedName}}]], Serilog.Settings.Configuration.Tests",
                    "value": {
                        "a": 1,
                        "b": 2
                    }
                }
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "Ctor");
        var value = new ObjectArgumentValue(section, []);

        var result = value.ConvertTo(typeof(IAmAnInterface), new());

        Assert.IsType(expectedType, result);
        var dictionary = Assert.IsType<Dictionary<string, int>>(valueProp?.GetValue(result));
        Assert.Equal(new Dictionary<string, int> { { "a", 1 }, { "b", 2 } }, dictionary);
    }

    [Fact]
    public void ConvertsToExplicitTypeWithStructConstructorArgument()
    {
        // language=json
        const string json = """
            {
                "Ctor": {
                    "type": "Serilog.Settings.Configuration.Tests.Support.GenericClass`1[[Serilog.Settings.Configuration.Tests.ObjectArgumentValueTests+PlainStruct, Serilog.Settings.Configuration.Tests]], Serilog.Settings.Configuration.Tests",
                    "value": { "A" : "1" }
                }
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "Ctor");
        var value = new ObjectArgumentValue(section, []);

        var result = value.ConvertTo(typeof(IAmAnInterface), new());

        var actual = Assert.IsType<GenericClass<PlainStruct>>(result);
        Assert.Equal("1", actual.Value.A);
        Assert.Null(actual.Value.B);
    }

    [Fact]
    public void ConvertsToExplicitTypeWithClassConstructorArgument()
    {
        // language=json
        const string json = """
            {
                "Ctor": {
                    "type": "Serilog.Settings.Configuration.Tests.Support.GenericClass`1[[TestDummies.DummyLoggerConfigurationExtensions+Binding, TestDummies]], Serilog.Settings.Configuration.Tests",
                    "value": { "foo" : "bar" }
                }
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "Ctor");
        var value = new ObjectArgumentValue(section, []);

        var result = value.ConvertTo(typeof(IAmAnInterface), new());

        var actual = Assert.IsType<GenericClass<TestDummies.DummyLoggerConfigurationExtensions.Binding>>(result);
        Assert.Equal("bar", actual.Value.Foo);
        Assert.Null(actual.Value.Abc);
    }

    readonly struct Struct : IAmAnInterface
    {
        public string String { get; }
        public Struct(string str) { String = str; }
    }

    [Fact]
    public void ConvertsToExplicitTypeWithExplicitStructConstructorArgument()
    {
        // language=json
        const string json = """
            {
                "Ctor": {
                    "type": "Serilog.Settings.Configuration.Tests.Support.GenericClass`1[[Serilog.Settings.Configuration.Tests.Support.IAmAnInterface, Serilog.Settings.Configuration.Tests]], Serilog.Settings.Configuration.Tests",
                    "value": {
                        "type": "Serilog.Settings.Configuration.Tests.ObjectArgumentValueTests+Struct, Serilog.Settings.Configuration.Tests",
                        "str" : "abc"
                    }
                }
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "Ctor");
        var value = new ObjectArgumentValue(section, []);

        var result = value.ConvertTo(typeof(IAmAnInterface), new());

        var actual = Assert.IsType<GenericClass<IAmAnInterface>>(result);
        var structValue = Assert.IsType<Struct>(actual.Value);
        Assert.Equal("abc", structValue.String);
    }

    [Fact]
    public void ConvertsToExplicitTypeWithExplicitTypeConstructorArgument()
    {
        // language=json
        const string json = """
            {
                "Ctor": {
                    "type": "Serilog.Settings.Configuration.Tests.Support.GenericClass`1[[Serilog.Settings.Configuration.Tests.Support.IAmAnInterface, Serilog.Settings.Configuration.Tests]], Serilog.Settings.Configuration.Tests",
                    "value": {
                        "type": "Serilog.Settings.Configuration.Tests.ObjectArgumentValueTests+PrivateImplWithPublicCtor, Serilog.Settings.Configuration.Tests"
                    }
                }
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "Ctor");
        var value = new ObjectArgumentValue(section, []);

        var result = value.ConvertTo(typeof(IAmAnInterface), new());

        var actual = Assert.IsType<GenericClass<IAmAnInterface>>(result);
        Assert.IsType<PrivateImplWithPublicCtor>(actual.Value);
    }

    // While ObjectArgumentValue supports converting to primitives, this is normally handled by StringArgumentValue
    // ObjectArgumentValue will not honor ConfigurationReaderOptions.FormatProvider, it will use InvariantCulture
    [Theory]
    [InlineData(typeof(bool), false, "false")]
    [InlineData(typeof(bool), true, "true")]
    [InlineData(typeof(sbyte), (sbyte)-1, "-1")]
    [InlineData(typeof(byte), (byte)2, "2")]
    [InlineData(typeof(short), (short)-3, "-3")]
    [InlineData(typeof(ushort), (ushort)4, "4")]
    [InlineData(typeof(int), -5, "-5")]
    [InlineData(typeof(uint), 6U, "6")]
    [InlineData(typeof(long), -7L, "-7")]
    [InlineData(typeof(ulong), 8UL, "8")]
    [InlineData(typeof(float), -9.1F, "-9.1")]
    [InlineData(typeof(double), 10.2D, "10.2")]
    public void ConvertsToPrimitives(Type type, object expected, string sectionValue)
    {
        // language=json
        var json = $$"""
            {
                "value": {{sectionValue}}
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "value");
        var value = new ObjectArgumentValue(section, []);

        var actual = value.ConvertTo(type, new());

        Assert.Equal(expected, actual);
    }

    // While ObjectArgumentValue supports converting to a nullable primitive, this is normally handled by StringArgumentValue
    // ObjectArgumentValue will not honor ConfigurationReaderOptions.FormatProvider, it will use InvariantCulture
    [Fact]
    public void ConvertsToNullablePrimitive()
    {
        // language=json
        const string json = """
            {
                "value": 123
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "value");
        var value = new ObjectArgumentValue(section, []);

        var actual = value.ConvertTo(typeof(int?), new());

        Assert.Equal(123, actual);
    }

    // While ObjectArgumentValue supports converting to a nullable primitive, this is normally handled by StringArgumentValue
    [Fact]
    public void ConvertsToNullWhenEmptyNullable()
    {
        // language=json
        const string json = """
            {
                "value": null
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "value");
        var value = new ObjectArgumentValue(section, []);

        var actual = value.ConvertTo(typeof(int?), new());

        Assert.Null(actual);
    }

    [Fact]
    public void ConvertsToPlainClass()
    {
        // language=json
        const string json = """
            {
                "value": { "foo" : "bar" }
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "value");
        var value = new ObjectArgumentValue(section, []);

        var actual = value.ConvertTo(typeof(TestDummies.DummyLoggerConfigurationExtensions.Binding), new());

        var binding = Assert.IsType<TestDummies.DummyLoggerConfigurationExtensions.Binding>(actual);
        Assert.Equal("bar", binding.Foo);
        Assert.Null(binding.Abc);
    }

    struct PlainStruct
    {
        public string? A { get; set; }
        public string? B { get; set; }
    }

    [Fact]
    public void ConvertsToPlainStruct()
    {
        // language=json
        const string json = """
            {
                "value": { "A" : "1" }
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "value");
        var value = new ObjectArgumentValue(section, []);

        var actual = value.ConvertTo(typeof(PlainStruct), new());

        var plain = Assert.IsType<PlainStruct>(actual);
        Assert.Equal("1", plain.A);
        Assert.Null(plain.B);
    }

    // While ObjectArgumentValue supports this, a null value is normally handled by StringArgumentValue
    // This is because IConfigurationSection will resolve null to an empty string
    // This behavior is under review, see https://github.com/dotnet/runtime/issues/36510
    [Fact]
    public void ConvertsToNullWhenStructIsNull()
    {
        // language=json
        const string json = """
            {
               "value": null
            }
            """;
        var section = JsonStringConfigSource.LoadSection(json, "value");
        var value = new ObjectArgumentValue(section, []);

        var actual = value.ConvertTo(typeof(PlainStruct), new());

        Assert.Null(actual);
    }

    // This is intended to mirror Serilog.Sinks.Email's options type.
    // https://github.com/serilog/serilog-settings-configuration/issues/417
    public class NestedComplexType
    {
        public string? Host { get; set; }
        public ITextFormatter? Subject { get; set; }
    }

    [Fact]
    public void ConstructsNestedComplexObjects()
    {
        // language=json
        const string json = """
            {
                "options": {
                  "subject": {
                    "type": "Serilog.Formatting.Display.MessageTemplateTextFormatter, Serilog",
                    "outputTemplate": "Serilog test"
                  },
                  "host": "localhost"
                }
            }
            """;

        var section = JsonStringConfigSource.LoadSection(json, "options");
        var value = new ObjectArgumentValue(section, []);

        var actual = AssertConvertsToType<NestedComplexType>(value);
        Assert.Equal("localhost", actual.Host);
        var formatter = Assert.IsType<MessageTemplateTextFormatter>(actual.Subject);
        var sw = new StringWriter();
        formatter.Format(Some.LogEvent(), sw);
        Assert.Equal("Serilog test", sw.ToString());
    }
}
