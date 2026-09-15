// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using DateTime = DateTime;
    using String = string;
    using TimeZoneInfo = TimeZoneInfo;

    public static partial class Extensions
    {
        public static DateTime ConvertTimeBySystemTimeZoneId(this DateTime dateTime, String destinationTimeZoneId)
        {
            return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(dateTime, destinationTimeZoneId);
        }

        public static DateTime ConvertTimeBySystemTimeZoneId(this DateTime dateTime, String sourceTimeZoneId,
            String destinationTimeZoneId)
        {
            return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(dateTime, sourceTimeZoneId, destinationTimeZoneId);
        }
    }
}
