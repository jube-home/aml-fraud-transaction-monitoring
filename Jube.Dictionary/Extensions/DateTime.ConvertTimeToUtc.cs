// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using DateTime = DateTime;
    using TimeZoneInfo = TimeZoneInfo;

    public static partial class Extensions
    {
        extension(DateTime dateTime)
        {
            [Description("Converts the time to UTC.")]
            public DateTime ConvertTimeToUtc()
            {
                return TimeZoneInfo.ConvertTimeToUtc(dateTime);
            }

            [Description("Converts the time from a specified time zone to UTC.")]
            public DateTime ConvertTimeToUtc([Description("The time zone of the dateTime.")] TimeZoneInfo sourceTimeZone)
            {
                return TimeZoneInfo.ConvertTimeToUtc(dateTime, sourceTimeZone);
            }
        }
    }
}
