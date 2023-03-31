
#if !NET6_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Interface | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter, Inherited = false)]
    internal sealed class DynamicallyAccessedMembersAttribute : Attribute
    {
        public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes)
        {
            MemberTypes = memberTypes;
        }

        public DynamicallyAccessedMemberTypes MemberTypes { get; }
    }

    [Flags]
    internal enum DynamicallyAccessedMemberTypes
    {
        //
        // Summary:
        //     Specifies all members.
        All = -1,
        //
        // Summary:
        //     Specifies no members.
        None = 0,
        //
        // Summary:
        //     Specifies the default, parameterless public constructor.
        PublicParameterlessConstructor = 1,
        //
        // Summary:
        //     Specifies all public constructors.
        PublicConstructors = 3,
        //
        // Summary:
        //     Specifies all non-public constructors.
        NonPublicConstructors = 4,
        //
        // Summary:
        //     Specifies all public methods.
        PublicMethods = 8,
        //
        // Summary:
        //     Specifies all non-public methods.
        NonPublicMethods = 16,
        //
        // Summary:
        //     Specifies all public fields.
        PublicFields = 32,
        //
        // Summary:
        //     Specifies all non-public fields.
        NonPublicFields = 64,
        //
        // Summary:
        //     Specifies all public nested types.
        PublicNestedTypes = 128,
        //
        // Summary:
        //     Specifies all non-public nested types.
        NonPublicNestedTypes = 256,
        //
        // Summary:
        //     Specifies all public properties.
        PublicProperties = 512,
        //
        // Summary:
        //     Specifies all non-public properties.
        NonPublicProperties = 1024,
        //
        // Summary:
        //     Specifies all public events.
        PublicEvents = 2048,
        //
        // Summary:
        //     Specifies all non-public events.
        NonPublicEvents = 4096,
        //
        // Summary:
        //     Specifies all interfaces implemented by the type.
        Interfaces = 8192
    }
}
#endif