// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using DateTime = DateTime;

    public static partial class Extensions
    {
        [Description("Checks if the date is equal to any in the provided array.")]
        public static bool In(this DateTime @this,
            [Description("The value list to compare with the object.")] params DateTime[] values)
        {
            return Array.IndexOf(values, @this) != -1;
        }
    }
}
