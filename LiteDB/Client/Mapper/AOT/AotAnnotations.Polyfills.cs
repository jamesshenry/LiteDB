#if !NET8_0_OR_GREATER
// Polyfills so the AOT annotations compile on legacy targets where
// the real attributes are not part of the reference assemblies.
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class |
        AttributeTargets.Assembly | AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
    internal sealed class RequiresUnreferencedCodeAttribute : Attribute
    {
        public RequiresUnreferencedCodeAttribute(string message) { this.Message = message; }
        public string Message { get; }
        public string Url { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class |
        AttributeTargets.Assembly | AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
    internal sealed class RequiresDynamicCodeAttribute : Attribute
    {
        public RequiresDynamicCodeAttribute(string message) { this.Message = message; }
        public string Message { get; }
        public string Url { get; set; }
    }
}
#endif