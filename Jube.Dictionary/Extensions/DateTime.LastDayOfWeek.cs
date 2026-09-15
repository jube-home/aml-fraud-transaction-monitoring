// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using DateTime = DateTime;

    public static partial class Extensions
    {
        public static DateTime LastDayOfWeek(this DateTime @this)
        {
            return new DateTime(@this.Year, @this.Month, @this.Day).AddDays(6 - (int)@this.DayOfWeek);
        }
    }
}
