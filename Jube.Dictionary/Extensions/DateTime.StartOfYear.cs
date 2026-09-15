// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using DateTime = DateTime;

    public static partial class Extensions
    {
        public static DateTime StartOfYear(this DateTime @this)
        {
            return new DateTime(@this.Year, 1, 1);
        }
    }
}
