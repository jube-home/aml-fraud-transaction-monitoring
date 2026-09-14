// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using DateTime = DateTime;

    public static partial class Extensions
    {
        public static DateTime SetTime(this DateTime current, int hour)
        {
            return current.SetTime(hour, 0, 0, 0);
        }

        public static DateTime SetTime(this DateTime current, int hour, int minute)
        {
            return current.SetTime(hour, minute, 0, 0);
        }

        public static DateTime SetTime(this DateTime current, int hour, int minute, int second)
        {
            return current.SetTime(hour, minute, second, 0);
        }

        public static DateTime SetTime(this DateTime current, int hour, int minute, int second, int millisecond)
        {
            return new DateTime(current.Year, current.Month, current.Day, hour, minute, second, millisecond);
        }
    }
}
