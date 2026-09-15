// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using DateTime = DateTime;
    using Int32 = int;

    public static partial class Extensions
    {
        public static Int32 DaysInMonth(this Int32 year, Int32 month)
        {
            return DateTime.DaysInMonth(year, month);
        }
    }
}
