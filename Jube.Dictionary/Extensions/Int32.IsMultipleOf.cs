// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Int32 = int;

    public static partial class Extensions
    {
        [Description("Checks if the number is a multiple of another.")]
        public static bool IsMultipleOf(this Int32 @this, [Description("The factor to check against.")] Int32 factor)
        {
            return @this % factor == 0;
        }
    }
}
