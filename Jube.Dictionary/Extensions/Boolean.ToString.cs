// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        [Description("Returns a custom string based on the boolean value.")]
        public static string ToString(this bool @this,
            [Description("The value to return if true.")] string trueValue,
            [Description("The value to return if false.")] string falseValue)
        {
            return @this ? trueValue : falseValue;
        }
    }
}
