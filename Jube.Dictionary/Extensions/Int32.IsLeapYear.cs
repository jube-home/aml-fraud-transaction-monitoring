// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Boolean = bool;
    using DateTime = DateTime;
    using Int32 = int;

    public static partial class Extensions
    {
        [Description("Checks if the year is a leap year.")]
        public static Boolean IsLeapYear(this Int32 year)
        {
            return DateTime.IsLeapYear(year);
        }
    }
}
