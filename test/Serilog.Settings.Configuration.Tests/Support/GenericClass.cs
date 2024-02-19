namespace Serilog.Settings.Configuration.Tests.Support;

public class GenericClass<T> : IAmAnInterface
{
    public T Value { get; }

    public GenericClass(T value)
    {
        Value = value;
    }
}
