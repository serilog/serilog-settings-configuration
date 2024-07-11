using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;
using Serilog.Settings.Configuration.Tests.Support;
using System.Globalization;
using System.Reflection;

namespace Serilog.Settings.Configuration.Tests;

public class StringArgumentValueTests
{
    [Fact]
    public void StringValuesConvertToDefaultInstancesIfTargetIsInterface()
    {
        var stringArgumentValue = new StringArgumentValue("Serilog.Formatting.Json.JsonFormatter, Serilog");

        var result = stringArgumentValue.ConvertTo(typeof(ITextFormatter), new ResolutionContext());

        Assert.IsType<JsonFormatter>(result);
    }

    [Fact]
    public void StringValuesConvertToDefaultInstancesIfTargetIsAbstractClass()
    {
        var stringArgumentValue = new StringArgumentValue("Serilog.Settings.Configuration.Tests.Support.ConcreteClass, Serilog.Settings.Configuration.Tests");

        var result = stringArgumentValue.ConvertTo(typeof(AbstractClass), new ResolutionContext());

        Assert.IsType<ConcreteClass>(result);
    }

    [Theory]
    [InlineData("My.NameSpace.Class+InnerClass::Member",
               "My.NameSpace.Class+InnerClass", "Member")]
    [InlineData("  TrimMe.NameSpace.Class::NeedsTrimming  ",
               "TrimMe.NameSpace.Class", "NeedsTrimming")]
    [InlineData("My.NameSpace.Class::Member",
               "My.NameSpace.Class", "Member")]
    [InlineData("My.NameSpace.Class::Member, MyAssembly",
               "My.NameSpace.Class, MyAssembly", "Member")]
    [InlineData("My.NameSpace.Class::Member, MyAssembly, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
               "My.NameSpace.Class, MyAssembly, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "Member")]
    [InlineData("Just a random string with :: in it",
               null, null)]
    [InlineData("Its::a::trapWithColonsAppearingTwice",
               null, null)]
    [InlineData("ThereIsNoMemberHere::",
               null, null)]
    [InlineData(null,
               null, null)]
    [InlineData(" ",
               null, null)]
    // a full-qualified type name should not be considered a static member accessor
    [InlineData("My.NameSpace.Class, MyAssembly, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
       null, null)]
    public void TryParseStaticMemberAccessorReturnsExpectedResults(string? input, string? expectedAccessorType, string? expectedPropertyName)
    {
        var actual = StringArgumentValue.TryParseStaticMemberAccessor(input,
            out var actualAccessorType,
            out var actualMemberName);

        if (expectedAccessorType == null)
        {
            Assert.False(actual, $"Should not parse {input}");
        }
        else
        {
            Assert.True(actual, $"should successfully parse {input}");
            Assert.Equal(expectedAccessorType, actualAccessorType);
            Assert.Equal(expectedPropertyName, actualMemberName);
        }
    }

    [Theory]
    [InlineData("Serilog.Formatting.Json.JsonFormatter", typeof(JsonFormatter))]
    [InlineData("Serilog.Formatting.Json.JsonFormatter, Serilog", typeof(JsonFormatter))]
    [InlineData("Serilog.ConfigurationLoggerConfigurationExtensions", typeof(ConfigurationLoggerConfigurationExtensions))]
    public void FindTypeSupportsSimpleNamesForSerilogTypes(string input, Type targetType)
    {
        var type = StringArgumentValue.FindType(input);
        Assert.Equal(targetType, type);
    }

    [Theory]
    [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::InterfaceProperty, Serilog.Settings.Configuration.Tests", typeof(IAmAnInterface))]
    [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::AbstractProperty, Serilog.Settings.Configuration.Tests", typeof(AnAbstractClass))]
    [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::InterfaceField, Serilog.Settings.Configuration.Tests", typeof(IAmAnInterface))]
    [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::AbstractField, Serilog.Settings.Configuration.Tests", typeof(AnAbstractClass))]
    public void StaticMembersAccessorsCanBeUsedForAbstractTypes(string input, Type targetType)
    {
        var stringArgumentValue = new StringArgumentValue(input);

        var actual = stringArgumentValue.ConvertTo(targetType, new ResolutionContext());

        Assert.IsAssignableFrom(targetType, actual);
        Assert.Equal(ConcreteImpl.Instance, actual);
    }

    [Fact]
    public void StaticMembersAccessorsCanBeUsedForMethodInfoWhenThereAreNoOverloads()
    {
        var stringArgumentValue = new StringArgumentValue("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::IntParseMethodNoOverloads, Serilog.Settings.Configuration.Tests");

        var actual = stringArgumentValue.ConvertTo(typeof(MethodInfo), new ResolutionContext());

        var parser = Assert.IsAssignableFrom<MethodInfo>(actual);
        Assert.Equal(100, parser.Invoke(null, ["100"]));
    }

    [Theory]
    [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::FuncIntParseField, Serilog.Settings.Configuration.Tests", typeof(Func<string, int>))]
    [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::NamedIntParseField, Serilog.Settings.Configuration.Tests", typeof(NamedIntParse))]
    [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::FuncIntParseProperty, Serilog.Settings.Configuration.Tests", typeof(Func<string, int>))]
    [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::NamedIntParseProperty, Serilog.Settings.Configuration.Tests", typeof(NamedIntParse))]
    [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::IntParseMethod, Serilog.Settings.Configuration.Tests", typeof(NamedIntParse))]
    [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::IntParseMethod, Serilog.Settings.Configuration.Tests", typeof(Func<string, int>))]
    public void StaticMembersAccessorsCanBeUsedForDelegateTypes(string input, Type targetType)
    {
        var stringArgumentValue = new StringArgumentValue(input);

        var actual = stringArgumentValue.ConvertTo(targetType, new ResolutionContext());

        Assert.IsAssignableFrom(targetType, actual);
        var parser = (Delegate?)actual;
        Assert.Equal(100, parser?.DynamicInvoke("100"));
    }

    [Theory]
    [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::ConcreteClassProperty, Serilog.Settings.Configuration.Tests", typeof(AConcreteClass))]
    [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::ConcreteClassField, Serilog.Settings.Configuration.Tests", typeof(AConcreteClass))]
    public void StaticMembersAccessorsCanBeUsedForConcreteReferenceTypes(string input, Type targetType)
    {
        var stringArgumentValue = new StringArgumentValue(input);

        var actual = stringArgumentValue.ConvertTo(targetType, new ResolutionContext());

        Assert.IsAssignableFrom(targetType, actual);
        Assert.Equal(ConcreteImplOfConcreteClass.Instance, actual);
    }

    [Theory]
    [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::IntProperty, Serilog.Settings.Configuration.Tests", typeof(int), 42)]
    [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::StringProperty, Serilog.Settings.Configuration.Tests", typeof(string),
        "Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::StringProperty, Serilog.Settings.Configuration.Tests")]
    public void StaticMembersAccessorsCanBeUsedForBuiltInTypes(string input, Type targetType, object expected)
    {
        var stringArgumentValue = new StringArgumentValue(input);

        var actual = stringArgumentValue.ConvertTo(targetType, new ResolutionContext());

        Assert.Equal(expected, actual);
    }

    [Theory]
    // unknown type
    [InlineData("Namespace.ThisIsNotAKnownType::InterfaceProperty, Serilog.Settings.Configuration.Tests", typeof(IAmAnInterface))]
    // good type name, but wrong namespace
    [InlineData("Random.Namespace.ClassWithStaticAccessors::InterfaceProperty, Serilog.Settings.Configuration.Tests", typeof(IAmAnInterface))]
    // good full type name, but missing or wrong assembly
    [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::InterfaceProperty", typeof(IAmAnInterface))]
    public void StaticAccessorOnUnknownTypeThrowsTypeLoadException(string input, Type targetType)
    {
        var stringArgumentValue = new StringArgumentValue($"{input}");
        Assert.Throws<TypeLoadException>(() =>
            stringArgumentValue.ConvertTo(targetType, new ResolutionContext())
        );
    }

    [Theory]
    // unknown member
    [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::UnknownMember, Serilog.Settings.Configuration.Tests", typeof(IAmAnInterface))]
    // static property exists but it's private
    [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::PrivateInterfaceProperty, Serilog.Settings.Configuration.Tests", typeof(IAmAnInterface))]
    // static field exists but it's private
    [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::PrivateInterfaceField, Serilog.Settings.Configuration.Tests", typeof(IAmAnInterface))]
    // public property exists but it's not static
    [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::InstanceInterfaceProperty, Serilog.Settings.Configuration.Tests", typeof(IAmAnInterface))]
    // public field exists but it's not static
    [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::InstanceInterfaceField, Serilog.Settings.Configuration.Tests", typeof(IAmAnInterface))]
    public void StaticAccessorWithInvalidMemberThrowsInvalidOperationException(string input, Type targetType)
    {
        var stringArgumentValue = new StringArgumentValue($"{input}");
        var exception = Assert.Throws<InvalidOperationException>(() =>
            stringArgumentValue.ConvertTo(targetType, new ResolutionContext())
        );

        Assert.Contains("Could not find a public static property or field ", exception.Message);
        Assert.Contains("on type `Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors, Serilog.Settings.Configuration.Tests`", exception.Message);
    }

    [Fact]
    public void LevelSwitchesCanBeLookedUpByName()
    {
        var @switch = new LoggingLevelSwitch(LogEventLevel.Verbose);
        var switchName = "$theSwitch";
        var resolutionContext = new ResolutionContext();
        resolutionContext.AddLevelSwitch(switchName, @switch);

        var stringArgumentValue = new StringArgumentValue(switchName);

        var resolvedSwitch = stringArgumentValue.ConvertTo(typeof(LoggingLevelSwitch), resolutionContext);

        Assert.IsType<LoggingLevelSwitch>(resolvedSwitch);
        Assert.Same(@switch, resolvedSwitch);
    }


    [Fact]
    public void ReferencingUndeclaredLevelSwitchThrows()
    {
        var resolutionContext = new ResolutionContext();
        resolutionContext.AddLevelSwitch("$anotherSwitch", new LoggingLevelSwitch(LogEventLevel.Verbose));

        var stringArgumentValue = new StringArgumentValue("$mySwitch");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            stringArgumentValue.ConvertTo(typeof(LoggingLevelSwitch), resolutionContext)
        );

        Assert.Contains("$mySwitch", ex.Message);
        Assert.Contains("\"LevelSwitches\":{\"$mySwitch\":", ex.Message);
    }

    [Fact]
    public void StringValuesConvertToEnumByName()
    {
        var value = new StringArgumentValue(nameof(LogEventLevel.Information));

        var actual = value.ConvertTo(typeof(LogEventLevel), new());

        Assert.Equal(LogEventLevel.Information, actual);
    }

    [Fact]
    public void StringValuesConvertToEnumByValue()
    {
        var value = new StringArgumentValue("2");

        var actual = value.ConvertTo(typeof(LogEventLevel), new());

        Assert.Equal(LogEventLevel.Information, actual);
    }

    [Fact]
    public void StringValuesConvertToUnwrappedNullable()
    {
        var value = new StringArgumentValue("123");

        var actual = value.ConvertTo(typeof(int?), new());

        Assert.Equal(123, actual);
    }

    [Fact]
    public void StringValuesConvertToNullWhenEmptyNullable()
    {
        var value = new StringArgumentValue("");

        var actual = value.ConvertTo(typeof(int?), new());

        Assert.Null(actual);
    }

    [Fact]
    public void StringValuesConvertToUri()
    {
        var stringArgumentValue = new StringArgumentValue("https://test.local");

        var actual = stringArgumentValue.ConvertTo(typeof(Uri), new ResolutionContext());

        Assert.Equal(new Uri("https://test.local"), actual as Uri);
    }

    [Fact]
    public void StringValuesConvertToTimespan()
    {
        var stringArgumentValue = new StringArgumentValue("1.23:45:30.1234567");

        var actual = stringArgumentValue.ConvertTo(typeof(TimeSpan), new ResolutionContext());

        Assert.Equal(TimeSpan.Parse("1.23:45:30.1234567"), actual);
    }

    [Fact]
    public void StringValuesConvertToTypeFromShortTypeName()
    {
        var shortTypeName = "System.Version";
        var stringArgumentValue = new StringArgumentValue(shortTypeName);

        var actual = (Type?)stringArgumentValue.ConvertTo(typeof(Type), new ResolutionContext());

        Assert.Equal(typeof(Version), actual);
    }

    [Fact]
    public void StringValuesConvertToTypeFromAssemblyQualifiedName()
    {
        var assemblyQualifiedName = typeof(Version).AssemblyQualifiedName!;
        var stringArgumentValue = new StringArgumentValue(assemblyQualifiedName);

        var actual = (Type?)stringArgumentValue.ConvertTo(typeof(Type), new ResolutionContext());

        Assert.Equal(typeof(Version), actual);
    }

    [Theory]
    [InlineData(typeof(bool), false, "False")]
    [InlineData(typeof(bool), true, "True")]
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
    public void StringValuesConvertToPrimitives(Type type, object expected, string sectionValue)
    {
        var value = new StringArgumentValue(sectionValue);

        var actual = value.ConvertTo(type, new());

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StringValuesConvertToPrimitivesUsingAlternativeFormatProvider()
    {
        var value = new StringArgumentValue("1.234,56");

        var formatProvider = new NumberFormatInfo
        {
            NumberDecimalSeparator = ",",
            NumberGroupSeparator = ".",
            NumberGroupSizes = [3],
        };

        var actual = value.ConvertTo(typeof(decimal), new(readerOptions: new() { FormatProvider = formatProvider }));

        Assert.Equal(1234.56M, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Just some string that is hard to misinterpret")]
    [InlineData("True")]
    [InlineData("10")]
    [InlineData("Information")]
    [InlineData("1.23:45:30.1234567")]
    [InlineData("https://test.local")]
    [InlineData("Serilog.Formatting.Json.JsonFormatter")]
    [InlineData("Serilog.Formatting.Json.JsonFormatter, Serilog")]
    [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::IntProperty, Serilog.Settings.Configuration.Tests")]
    [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::StringProperty, Serilog.Settings.Configuration.Tests")]
    [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::InterfaceProperty, Serilog.Settings.Configuration.Tests")]
    [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::AbstractProperty, Serilog.Settings.Configuration.Tests")]
    [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::InterfaceField, Serilog.Settings.Configuration.Tests")]
    [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::AbstractField, Serilog.Settings.Configuration.Tests")]
    [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::FuncIntParseField, Serilog.Settings.Configuration.Tests")]
    [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::NamedIntParseField, Serilog.Settings.Configuration.Tests")]
    [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::FuncIntParseProperty, Serilog.Settings.Configuration.Tests")]
    [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::NamedIntParseProperty, Serilog.Settings.Configuration.Tests")]
    [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::IntParseMethod, Serilog.Settings.Configuration.Tests")]
    public void StringValuesConvertToString(string expected)
    {
        var value = new StringArgumentValue(expected);

        var actual = value.ConvertTo(typeof(string), new());

        Assert.Equal(expected, actual);
    }
}
