namespace Serilog.Settings.Configuration.Tests.Support
{
    public interface IAmAnInterface
    {
    }

    public abstract class AnAbstractClass
    {
    }

    class ConcreteImpl : AnAbstractClass, IAmAnInterface
    {
        private ConcreteImpl()
        {
        }

        public static ConcreteImpl Instance { get; } = new ConcreteImpl();
    }

    public class AConcreteClass
    {
    }

    class ConcreteImplOfConcreteClass : AConcreteClass
    {
        public static ConcreteImplOfConcreteClass Instance { get; } = new ConcreteImplOfConcreteClass();
    }

    public class ClassWithStaticAccessors
    {
        public static IAmAnInterface InterfaceProperty => ConcreteImpl.Instance;
        public static AnAbstractClass AbstractProperty => ConcreteImpl.Instance;
        public static AConcreteClass ConcreteClassProperty => ConcreteImplOfConcreteClass.Instance;
        public static int IntProperty => 42;
        public static string StringProperty => "don't see me";

        public static IAmAnInterface InterfaceField = ConcreteImpl.Instance;
        public static AnAbstractClass AbstractField = ConcreteImpl.Instance;
        public static AConcreteClass ConcreteClassField = ConcreteImplOfConcreteClass.Instance;

        // ReSharper disable once UnusedMember.Local
        private static IAmAnInterface PrivateInterfaceProperty => ConcreteImpl.Instance;

#pragma warning disable 169
        private static IAmAnInterface PrivateInterfaceField = ConcreteImpl.Instance;
#pragma warning restore 169
        public IAmAnInterface InstanceInterfaceProperty => ConcreteImpl.Instance;
        public IAmAnInterface InstanceInterfaceField = ConcreteImpl.Instance;
    }
}
