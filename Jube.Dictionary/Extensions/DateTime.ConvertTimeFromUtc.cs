// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using DateTime = DateTime;
    using TimeZoneInfo = TimeZoneInfo;

    public static partial class Extensions
    {
        [Description("Converts UTC time to a specified time zone.")]
        public static DateTime ConvertTimeFromUtc(this DateTime dateTime,
            [Description("The time zone to convert to.")] TimeZoneInfo destinationTimeZone)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime, destinationTimeZone);
        }
    }
}
